using System.Data;
using Microsoft.EntityFrameworkCore;
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
    private readonly IRegistrationDepositRefundService _depositRefundService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IOrderService _orderService;
    private readonly IBidService _bidService;

    public OrderCreationService(
        AuctionHouseDbContext dbContext,
        ILogger<OrderCreationService> logger,
        INotificationService notificationService,
        IRegistrationDepositRefundService depositRefundService,
        IRealtimePublisher realtimePublisher,
        IOrderService orderService,
        IBidService bidService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notificationService = notificationService;
        // Service này dùng để tự động hoàn tiền cọc cho loser khi auction kết thúc
        _depositRefundService = depositRefundService;
        _realtimePublisher = realtimePublisher;
        _orderService = orderService;
        _bidService = bidService;
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

        var existingOrderId = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.AuctionId == auctionId)
            .Select(item => (int?)item.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOrderId.HasValue)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOrderId;
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        if (auction is null)
        {
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

// Giá thắng của phiên đấu giá
        var subtotal = winningBid.Amount;

// ------------------------------------------------------------
// Lấy tiền cọc đã thanh toán của winner.
//
// Chỉ lấy deposit status = paid.
// Không lấy pending/cancelled/refunded.
// Nếu không có deposit thì depositAmount = 0.
// ------------------------------------------------------------
        var winnerDeposit = await _dbContext.AuctionRegistrationDeposits
            .FirstOrDefaultAsync(d =>
                    d.AuctionId == auctionId &&
                    d.UserId == winningBid.BidderId &&
                    d.Status == AuctionRegistrationDepositStatuses.Paid,
                cancellationToken);

        var depositAmount = winnerDeposit?.Amount ?? 0m;

// Phí bảo hiểm vault hiện có của hệ thống
        var insurance = Math.Round(Math.Max(60m, subtotal * 0.00721m), 2);

// Tổng tiền gốc trước khi trừ cọc
        var totalBeforeDeposit = subtotal + ShippingFee + insurance;

// ------------------------------------------------------------
// Trừ tiền cọc vào tổng tiền winner cần thanh toán.
//
// Ví dụ:
// Winning bid = 500
// Shipping = 45
// Insurance = 60
// Deposit = 50
//
// TotalAmount = 500 + 45 + 60 - 50 = 555
// ------------------------------------------------------------
        var total = Math.Max(0, totalBeforeDeposit - depositAmount);

        var order = new AuctionOrder
        {
            OrderReference = $"AH-{now:yyyyMMdd}-{auction.Id}",
            BuyerId = winningBid.BidderId,

            // Giá thắng
            Subtotal = subtotal,

            // Phí ship
            ShippingFee = ShippingFee,

            // Phí bảo hiểm
            VaultInsurance = insurance,

            // Tiền cọc của winner được trừ vào order
            DepositApplied = depositAmount,

            // Số tiền winner còn phải thanh toán sau khi trừ cọc
            TotalAmount = total,

            Status = OrderStatuses.PendingPayment,
            PaymentDeadline = now.AddHours(48),
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
                .Where(item => item.AuctionId == auctionId)
                .Select(item => (int?)item.OrderId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await _notificationService.CreateAndPushAsync(
            winningBid.BidderId,
            "You won the auction!",
            $"Congratulations! You won {auction.Product.Name}. Complete payment within 48 hours.",
            NotificationType.Winning,
            "/Order",
            NotificationReferenceTypes.AuctionWon,
            auction.Id,
            cancellationToken: cancellationToken);

        var orderCount = await _orderService.CountPendingPaymentOrdersAsync(winningBid.BidderId);
        await _realtimePublisher.SendOrderCountToUserAsync(winningBid.BidderId, orderCount, cancellationToken);

        var bidState = await _bidService.GetBidStateAsync(auction.Id, cancellationToken);
        if (bidState is not null)
        {
            await _realtimePublisher.SendBidUpdateAsync(auction.Id, bidState, cancellationToken);
        }

        return order.Id;
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
        var total = subtotal + ShippingFee + insurance;

        var order = new AuctionOrder
        {
            OrderReference = $"BN-{now:yyyyMMdd}-{auction.Id}",
            BuyerId = buyerId,
            Subtotal = subtotal,
            ShippingFee = ShippingFee,
            VaultInsurance = insurance,
            DepositApplied = 0m,
            TotalAmount = total,
            Status = OrderStatuses.PendingPayment,
            PaymentDeadline = now.AddHours(48),
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

        var orderCount = await _orderService.CountPendingPaymentOrdersAsync(buyerId);
        await _realtimePublisher.SendOrderCountToUserAsync(buyerId, orderCount, cancellationToken);

        return (true, "Added to your orders.");
    }
}
