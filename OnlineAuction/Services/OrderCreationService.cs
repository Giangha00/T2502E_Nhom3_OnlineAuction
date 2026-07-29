using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderCreationService : IOrderCreationService
{
    private const decimal ShippingFee = 45m;

    private readonly AuctionHouseDbContext _dbContext;
    private readonly ILogger<OrderCreationService> _logger;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly IRegistrationDepositRefundService _depositRefundService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IBidService _bidService;
    private readonly PlatformFeeSettings _feeSettings;

    public OrderCreationService(
        AuctionHouseDbContext dbContext,
        ILogger<OrderCreationService> logger,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        IRegistrationDepositRefundService depositRefundService,
        IRealtimePublisher realtimePublisher,
        IBidService bidService,
        IOptions<PlatformFeeSettings> feeSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _depositRefundService = depositRefundService;
        _realtimePublisher = realtimePublisher;
        _bidService = bidService;
        _feeSettings = feeSettings.Value;
    }

    public async Task<int> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var candidateAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.ListingType == ListingTypes.Auction &&
                (auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon) &&
                auction.DeletedAt == null)
            .Select(auction => new { auction.Id, auction.EndDate })
            .ToListAsync(cancellationToken);

        var auctionIds = candidateAuctions
            .Where(auction => !DateTimeUtilities.IsInFutureUtc(auction.EndDate))
            .Select(auction => auction.Id)
            .ToList();

        var createdCount = 0;

        foreach (var auctionId in auctionIds)
        {
            // Bước 1:
            // Finalize auction:
            // - Nếu có người thắng: tạo order pending payment
            // - Set auction.Status = AwaitingPayment
            // - Set auction.WinnerId = winningBid.BidderId
            // - Gửi notification cho winner
            //
            // Nếu không có bid:
            // - Set auction.Status = Ended
            var orderId = await CreatePendingPaymentOrderForAuctionAsync(
                auctionId,
                cancellationToken);

            if (orderId.HasValue)
            {
                createdCount++;
            }

            // Bước 2:
            // Sau khi auction đã finalize xong, tự động refund cọc cho người thua.
            //
            // Lý do đặt ở đây:
            // - Lúc này auction.WinnerId đã được lưu vào database
            // - Refund service có thể biết ai là winner để không refund cho winner
            // - Không gọi PayPal refund bên trong transaction tạo order
            //   vì PayPal là API bên ngoài, nếu chậm/lỗi sẽ làm transaction DB bị giữ lâu.
            //
            // RefundLoserDepositsForAuctionAsync sẽ:
            // - Lấy deposit status = paid
            // - Bỏ qua user winner
            // - Gọi PayPal refund
            // - Update deposit.status = refunded
            // - Lưu paypal_refund_id
            var refundedCount = await _depositRefundService
                .RefundLoserDepositsForAuctionAsync(auctionId, cancellationToken);

            if (refundedCount > 0)
            {
                _logger.LogInformation(
                    "Auto refunded {RefundedCount} loser deposits for auction {AuctionId}.",
                    refundedCount,
                    auctionId);
            }
        }

        return createdCount;
    }

    public Task<int?> CreatePendingPaymentOrderForAuctionAsync(
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => CreatePendingPaymentOrderCoreAsync(auctionId, cancellationToken));
    }

    private async Task<int?> CreatePendingPaymentOrderCoreAsync(
        int auctionId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var existingActiveOrderId = await _dbContext.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
            .Where(item =>
                item.AuctionId == auctionId &&
                item.Order.DeletedAt == null &&
                item.Order.Status != OrderStatuses.Cancelled)
            .Select(item => (int?)item.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingActiveOrderId.HasValue)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingActiveOrderId;
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var shouldFinalize = auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon
            && !DateTimeUtilities.IsInFutureUtc(auction.EndDate);

        if (!shouldFinalize)
        {
            _logger.LogInformation(
                "Skipping auction {AuctionId} finalization due to state re-check: status={Status}, endDate={EndDateUtc}, future={IsFuture}.",
                auction.Id,
                auction.Status,
                auction.EndDate,
                DateTimeUtilities.IsInFutureUtc(auction.EndDate));

            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var winningBid = await _dbContext.Bids
            .Where(bid => bid.AuctionId == auctionId && bid.IsWinning)
            .OrderByDescending(bid => bid.Amount)
            .ThenByDescending(bid => bid.PlacedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (winningBid is null)
        {
            auction.Status = AuctionStatuses.Ended;
            auction.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        var orderBuild = await BuildAuctionWinOrderAsync(
            auction,
            winningBid,
            now,
            paymentDeadlineHours: 48,
            cancellationToken: cancellationToken);
        if (orderBuild is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var order = orderBuild.Order;
        _dbContext.Orders.Add(order);
        auction.Status = order.Status == OrderStatuses.Paid
            ? AuctionStatuses.Completed
            : AuctionStatuses.AwaitingPayment;
        auction.WinnerId = winningBid.BidderId;
        auction.UpdatedAt = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Order creation raced for auction {AuctionId}.", auctionId);
            await transaction.RollbackAsync(cancellationToken);

            return await _dbContext.OrderItems
                .AsNoTracking()
                .Include(item => item.Order)
                .Where(item =>
                    item.AuctionId == auctionId &&
                    item.Order.DeletedAt == null &&
                    item.Order.Status != OrderStatuses.Cancelled)
                .Select(item => (int?)item.OrderId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (orderBuild.ExcessDepositRefundAmount > 0 && orderBuild.WinnerDepositId.HasValue)
        {
            var refundResult = await _depositRefundService.RefundDepositAmountAsync(
                orderBuild.WinnerDepositId.Value,
                orderBuild.ExcessDepositRefundAmount,
                cancellationToken: cancellationToken);

            if (!refundResult.Success)
            {
                _logger.LogWarning(
                    "Excess deposit refund failed for auction {AuctionId}, deposit {DepositId}: {Message}",
                    auctionId,
                    orderBuild.WinnerDepositId.Value,
                    refundResult.Message);
            }
        }

        await NotifyAuctionWinAsync(
            auction,
            order,
            winningBid.BidderId,
            order.Status == OrderStatuses.Paid,
            cancellationToken);
        return order.Id;
    }

    public async Task<bool> TryCreatePendingPaymentOrderWithinUnitOfWorkAsync(
        int auctionId,
        DateTime now,
        int paymentDeadlineHours,
        int? excludingCancelledOrderId = null,
        long? winningBidId = null,
        CancellationToken cancellationToken = default)
    {
        var hasActiveOrder = await _dbContext.OrderItems
            .Include(item => item.Order)
            .AnyAsync(item =>
                    item.AuctionId == auctionId &&
                    item.Order.DeletedAt == null &&
                    item.Order.Status != OrderStatuses.Cancelled &&
                    (!excludingCancelledOrderId.HasValue || item.Order.Id != excludingCancelledOrderId.Value),
                cancellationToken);

        if (hasActiveOrder)
        {
            return false;
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            return false;
        }

        var winningBid = winningBidId.HasValue
            ? await _dbContext.Bids.FirstOrDefaultAsync(
                bid => bid.Id == winningBidId.Value && bid.AuctionId == auctionId,
                cancellationToken)
            : await _dbContext.Bids
                .Where(bid => bid.AuctionId == auctionId && bid.IsWinning)
                .OrderByDescending(bid => bid.Amount)
                .ThenByDescending(bid => bid.PlacedAt)
                .FirstOrDefaultAsync(cancellationToken);

        if (winningBid is null)
        {
            return false;
        }

        var orderBuild = await BuildAuctionWinOrderAsync(
            auction,
            winningBid,
            now,
            paymentDeadlineHours,
            excludingCancelledOrderId,
            cancellationToken);

        if (orderBuild is null)
        {
            return false;
        }

        var order = orderBuild.Order;
        _dbContext.Orders.Add(order);
        auction.Status = order.Status == OrderStatuses.Paid
            ? AuctionStatuses.Completed
            : AuctionStatuses.AwaitingPayment;
        auction.WinnerId = winningBid.BidderId;
        auction.UpdatedAt = now;

        return true;
    }

    private async Task<AuctionWinOrderBuildResult?> BuildAuctionWinOrderAsync(
        Auction auction,
        Bid winningBid,
        DateTime now,
        int paymentDeadlineHours,
        int? excludingCancelledOrderId = null,
        CancellationToken cancellationToken = default)
    {
        var subtotal = winningBid.Amount;

        var winnerDeposit = await _dbContext.AuctionRegistrationDeposits
            .FirstOrDefaultAsync(d =>
                    d.AuctionId == auction.Id &&
                    d.UserId == winningBid.BidderId &&
                    d.Status == AuctionRegistrationDepositStatuses.Paid,
                cancellationToken);

        var depositAmount = winnerDeposit?.Amount ?? 0m;
        var insurance = Math.Round(Math.Max(60m, subtotal * 0.00721m), 2);
        var buyerCheckoutFee = MarketplaceFeeCalculator.CalculateBuyerCheckoutFee(subtotal, _feeSettings);
        var totalBeforeDeposit = subtotal + ShippingFee + insurance + buyerCheckoutFee;
        var depositApplied = Math.Min(depositAmount, totalBeforeDeposit);
        var total = Math.Max(0, totalBeforeDeposit - depositApplied);
        var isCoveredByDeposit = winnerDeposit is not null && total == 0m;
        var excessDepositRefundAmount = Math.Max(0, depositAmount - depositApplied);

        var priorCancelledCount = await _dbContext.OrderItems
            .Include(item => item.Order)
            .CountAsync(item =>
                    item.AuctionId == auction.Id &&
                    (item.Order.Status == OrderStatuses.Cancelled ||
                     (excludingCancelledOrderId.HasValue && item.Order.Id == excludingCancelledOrderId.Value)),
                cancellationToken);

        var recoverySuffix = priorCancelledCount > 0 ? $"-SC{priorCancelledCount + 1}" : string.Empty;

        if (isCoveredByDeposit && winnerDeposit is not null)
        {
            winnerDeposit.Status = AuctionRegistrationDepositStatuses.Applied;
            winnerDeposit.UpdatedAt = now;
        }

        var order = new AuctionOrder
        {
            OrderReference = $"AH-{now:yyyyMMdd}-{auction.Id}{recoverySuffix}",
            BuyerId = winningBid.BidderId,
            Subtotal = subtotal,
            ShippingFee = ShippingFee,
            VaultInsurance = insurance,
            PlatformFee = buyerCheckoutFee,
            DepositApplied = depositApplied,
            TotalAmount = total,
            Status = isCoveredByDeposit ? OrderStatuses.Paid : OrderStatuses.PendingPayment,
            OrderSource = OrderSources.AuctionWin,
            PaymentDeadline = now.AddHours(paymentDeadlineHours),
            PaymentMethod = isCoveredByDeposit ? "deposit" : null,
            CreatedAt = now,
            Items =
            [
                new OrderItem
                {
                    AuctionId = auction.Id,
                    ItemName = auction.Product.Name,
                    ItemGrade = auction.Product.GradeLabel,
                    ItemImageUrl = auction.Product.PrimaryImage,
                    WinningBid = subtotal,
                    CreatedAt = now
                }
            ]
        };

        if (isCoveredByDeposit)
        {
            MarketplaceFeeCalculator.ApplySellerSettlement(order, _feeSettings);
        }

        return new AuctionWinOrderBuildResult(
            order,
            winnerDeposit?.Id,
            excessDepositRefundAmount);
    }

    private sealed record AuctionWinOrderBuildResult(
        AuctionOrder Order,
        long? WinnerDepositId,
        decimal ExcessDepositRefundAmount);

    private async Task NotifyAuctionWinAsync(
        Auction auction,
        AuctionOrder order,
        int winningBidderId,
        bool paidByDeposit,
        CancellationToken cancellationToken)
    {
        await _notificationService.CreateAndPushAsync(
            winningBidderId,
            _notifyLocalizer[NotificationKeys.AuctionWonTitle],
            paidByDeposit
                ? _notifyLocalizer.Format(NotificationKeys.AuctionWonDepositCoveredMessage, auction.Product.Name)
                : _notifyLocalizer.Format(NotificationKeys.AuctionWonMessage, auction.Product.Name),
            NotificationType.Winning,
            paidByDeposit ? "/Account/PurchaseHistory" : "/Order",
            NotificationReferenceTypes.AuctionWon,
            auction.Id,
            cancellationToken: cancellationToken);

        if (paidByDeposit)
        {
            await OrderNotificationHelper.NotifySellerPaymentReceivedAsync(
                _notificationService,
                _notifyLocalizer,
                _dbContext,
                order.Id,
                "deposit",
                cancellationToken);
        }
        else
        {
            await OrderNotificationHelper.NotifySellerAwaitingPaymentAsync(
                _notificationService,
                _notifyLocalizer,
                _dbContext,
                order,
                cancellationToken);
        }

        var orderCount = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(o =>
                o.BuyerId == winningBidderId &&
                o.Status == OrderStatuses.PendingPayment &&
                o.DeletedAt == null,
                cancellationToken);
        await _realtimePublisher.SendOrderCountToUserAsync(winningBidderId, orderCount, cancellationToken);

        var bidState = await _bidService.GetBidStateAsync(auction.Id, cancellationToken);
        if (bidState is not null)
        {
            await _realtimePublisher.SendBidUpdateAsync(auction.Id, bidState, cancellationToken);
        }
    }

    public Task<(bool Success, string Message)> CreatePendingPaymentOrderForBuyNowAsync(
        int auctionId,
        int buyerId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() =>
            CreatePendingPaymentOrderForBuyNowCoreAsync(auctionId, buyerId, cancellationToken));
    }

    private async Task<(bool Success, string Message)> CreatePendingPaymentOrderForBuyNowCoreAsync(
        int auctionId,
        int buyerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        if (auction?.Product is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Product not found.");
        }

        if (auction.BuyNowPrice is null || auction.BuyNowPrice <= 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "This listing does not offer buy now.");
        }

        if (auction.Product.SellerId == buyerId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "You cannot purchase your own listing.");
        }

        var now = DateTime.UtcNow;
        var isLive = (auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon)
            && DateTimeUtilities.IsInFutureUtc(auction.EndDate);

        if (!isLive && auction.Status != AuctionStatuses.AwaitingPayment)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "This item is no longer available.");
        }

        var existingOrder = await _dbContext.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
            .Where(item => item.AuctionId == auctionId)
            .Select(item => new { item.OrderId, item.Order.BuyerId, item.Order.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOrder is not null)
        {
            if (existingOrder.BuyerId == buyerId
                && existingOrder.Status == OrderStatuses.PendingPayment)
            {
                await transaction.CommitAsync(cancellationToken);
                return (true, "Item is already in your orders.");
            }

            await transaction.RollbackAsync(cancellationToken);
            return (false, "This item is reserved by another buyer.");
        }

        if (auction.Status == AuctionStatuses.AwaitingPayment && auction.WinnerId != buyerId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "This item is reserved by another buyer.");
        }

        var subtotal = auction.BuyNowPrice.Value;

        var insurance = Math.Round(Math.Max(60m, subtotal * 0.00721m), 2);
        var buyerCheckoutFee = MarketplaceFeeCalculator.CalculateBuyerCheckoutFee(subtotal, _feeSettings);
        var total = subtotal + ShippingFee + insurance + buyerCheckoutFee;

        var order = new AuctionOrder
        {
            OrderReference = $"BN-{now:yyyyMMdd}-{auction.Id}",
            BuyerId = buyerId,
            Subtotal = subtotal,
            ShippingFee = ShippingFee,
            VaultInsurance = insurance,
            PlatformFee = buyerCheckoutFee,
            DepositApplied = 0m,
            TotalAmount = total,
            Status = OrderStatuses.PendingPayment,
            OrderSource = OrderSources.BuyNow,
            PaymentDeadline = now.AddDays(7),
            CreatedAt = now,
            Items =
            [
                new OrderItem
                {
                    AuctionId = auction.Id,
                    ItemName = auction.Product.Name,
                    ItemGrade = auction.Product.GradeLabel,
                    ItemImageUrl = auction.Product.PrimaryImage,
                    WinningBid = subtotal,
                    CreatedAt = now
                }
            ]
        };

        _dbContext.Orders.Add(order);
        auction.Status = AuctionStatuses.AwaitingPayment;
        auction.WinnerId = buyerId;
        auction.UpdatedAt = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Buy now order creation raced for auction {AuctionId}.", auctionId);
            await transaction.RollbackAsync(cancellationToken);
            return (true, "Item is already in your orders.");
        }

        await _notificationService.CreateAndPushAsync(
            buyerId,
            _notifyLocalizer[NotificationKeys.BuyNowOrderCreatedTitle],
            _notifyLocalizer.Format(NotificationKeys.BuyNowOrderCreatedMessage, auction.Product.Name),
            NotificationType.Payment,
            "/Order",
            NotificationReferenceTypes.BuyNowOrderCreated,
            order.Id,
            cancellationToken: cancellationToken);

        await OrderNotificationHelper.NotifySellerAwaitingPaymentAsync(
            _notificationService,
            _notifyLocalizer,
            _dbContext,
            order,
            cancellationToken);

        var orderCount = await _dbContext.Orders
            .AsNoTracking()
            .CountAsync(pendingOrder =>
                pendingOrder.BuyerId == buyerId &&
                pendingOrder.Status == OrderStatuses.PendingPayment &&
                pendingOrder.DeletedAt == null &&
                pendingOrder.PaymentDeadline > now &&
                pendingOrder.Items.Any(),
                cancellationToken);
        await _realtimePublisher.SendOrderCountToUserAsync(buyerId, orderCount, cancellationToken);

        return (true, "Added to your orders.");
    }
}
