using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class WinnerNonPaymentRecoveryService : IWinnerNonPaymentRecoveryService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IOrderCreationService _orderCreationService;
    private readonly INotificationService _notificationService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IBidService _bidService;
    private readonly WinnerNonPaymentSettings _settings;
    private readonly ILogger<WinnerNonPaymentRecoveryService> _logger;

    public WinnerNonPaymentRecoveryService(
        AuctionHouseDbContext dbContext,
        IOrderCreationService orderCreationService,
        INotificationService notificationService,
        IRealtimePublisher realtimePublisher,
        IBidService bidService,
        IOptions<WinnerNonPaymentSettings> settings,
        ILogger<WinnerNonPaymentRecoveryService> logger)
    {
        _dbContext = dbContext;
        _orderCreationService = orderCreationService;
        _notificationService = notificationService;
        _realtimePublisher = realtimePublisher;
        _bidService = bidService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessExpiredAuctionWinOrderAsync(
        AuctionOrder cancelledOrder,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (OrderCheckoutSelection.ResolveOrderSource(cancelledOrder) != OrderSources.AuctionWin)
        {
            return;
        }

        var auctionId = cancelledOrder.Items.FirstOrDefault()?.AuctionId;
        if (!auctionId.HasValue)
        {
            return;
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId.Value, cancellationToken);

        if (auction?.Product is null)
        {
            return;
        }

        var defaultingUserId = cancelledOrder.BuyerId;
        var productName = auction.Product.Name;
        var sellerId = auction.Product.SellerId;

        await LogAsync(
            auctionId.Value,
            cancelledOrder.Id,
            defaultingUserId,
            WinnerNonPaymentActions.PaymentExpired,
            $"Auction-win order {cancelledOrder.OrderReference} expired without payment.",
            cancellationToken: cancellationToken);

        var forfeitedDeposit = await ForfeitWinnerDepositAsync(
            auctionId.Value,
            defaultingUserId,
            now,
            cancellationToken);

        if (forfeitedDeposit is not null)
        {
            await LogAsync(
                auctionId.Value,
                cancelledOrder.Id,
                defaultingUserId,
                WinnerNonPaymentActions.DepositForfeited,
                $"Deposit #{forfeitedDeposit.Id} (${forfeitedDeposit.Amount:N2}) forfeited.",
                forfeitedDepositId: forfeitedDeposit.Id,
                forfeitedAmount: forfeitedDeposit.Amount,
                cancellationToken: cancellationToken);
        }

        await _notificationService.CreateAndPushAsync(
            defaultingUserId,
            "Payment deadline expired",
            $"You did not complete payment for {productName} within 48 hours. " +
            (forfeitedDeposit is not null
                ? $"Your registration deposit of ${forfeitedDeposit.Amount:N0} has been forfeited per platform policy."
                : "This auction win has expired."),
            NotificationType.Payment,
            "/Order",
            NotificationReferenceTypes.AuctionPaymentExpired,
            auctionId.Value,
            cancellationToken: cancellationToken);

        var excludedBidderIds = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order =>
                order.OrderSource == OrderSources.AuctionWin &&
                order.Status == OrderStatuses.Cancelled &&
                order.Items.Any(item => item.AuctionId == auctionId.Value))
            .Select(order => order.BuyerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!excludedBidderIds.Contains(defaultingUserId))
        {
            excludedBidderIds.Add(defaultingUserId);
        }

        var bids = await _dbContext.Bids
            .Where(bid => bid.AuctionId == auctionId.Value)
            .ToListAsync(cancellationToken);

        var runnerUp = WinnerNonPaymentBidSelector.SelectRunnerUp(bids, excludedBidderIds);

        if (runnerUp is not null)
        {
            foreach (var bid in bids)
            {
                bid.IsWinning = bid.Id == runnerUp.Id;
                bid.UpdatedAt = now;
            }

            auction.UpdatedAt = now;

            var secondChanceCreated = await _orderCreationService.TryCreatePendingPaymentOrderWithinUnitOfWorkAsync(
                auctionId.Value,
                now,
                _settings.SecondChancePaymentHours,
                cancellationToken);

            if (secondChanceCreated)
            {
                await LogAsync(
                    auctionId.Value,
                    cancelledOrder.Id,
                    defaultingUserId,
                    WinnerNonPaymentActions.SecondChanceOffered,
                    $"Second-chance order created for bidder #{runnerUp.BidderId}.",
                    secondChanceUserId: runnerUp.BidderId,
                    cancellationToken: cancellationToken);

                await _notificationService.CreateAndPushAsync(
                    runnerUp.BidderId,
                    "Second chance to win",
                    $"The original winner did not pay for {productName}. You are now the highest eligible bidder. " +
                    $"Complete payment within {_settings.SecondChancePaymentHours} hours.",
                    NotificationType.Winning,
                    "/Order",
                    NotificationReferenceTypes.AuctionSecondChanceOffered,
                    auctionId.Value,
                    cancellationToken: cancellationToken);

                await _notificationService.CreateAndPushAsync(
                    sellerId,
                    "Buyer did not pay",
                    $"The winning buyer did not pay for {productName}. A second-chance offer has been sent to the next highest bidder.",
                    NotificationType.Auction,
                    $"/Sell/MyAuctions",
                    NotificationReferenceTypes.AuctionPaymentExpired,
                    auctionId.Value,
                    cancellationToken: cancellationToken);

                var orderCount = await _dbContext.Orders
                    .AsNoTracking()
                    .CountAsync(order =>
                        order.BuyerId == runnerUp.BidderId &&
                        order.Status == OrderStatuses.PendingPayment &&
                        order.DeletedAt == null,
                        cancellationToken);
                await _realtimePublisher.SendOrderCountToUserAsync(runnerUp.BidderId, orderCount, cancellationToken);

                var bidState = await _bidService.GetBidStateAsync(auctionId.Value, cancellationToken);
                if (bidState is not null)
                {
                    await _realtimePublisher.SendBidUpdateAsync(auctionId.Value, bidState, cancellationToken);
                }

                _logger.LogInformation(
                    "Second-chance recovery created a new order for auction {AuctionId} after defaulting buyer {BuyerId}.",
                    auctionId.Value,
                    defaultingUserId);

                return;
            }
        }

        auction.Status = AuctionStatuses.Ended;
        auction.WinnerId = null;
        auction.UpdatedAt = now;

        foreach (var bid in bids.Where(bid => bid.IsWinning))
        {
            bid.IsWinning = false;
            bid.UpdatedAt = now;
        }

        await LogAsync(
            auctionId.Value,
            cancelledOrder.Id,
            defaultingUserId,
            WinnerNonPaymentActions.RelistRecommended,
            "No eligible runner-up bidder found. Auction closed for seller relist.",
            cancellationToken: cancellationToken);

        await _notificationService.CreateAndPushAsync(
            sellerId,
            "Buyer did not pay — relist available",
            $"The winning buyer did not pay for {productName}. No eligible second-chance bidder was available. You can relist this item.",
            NotificationType.Auction,
            "/Sell/MyAuctions",
            NotificationReferenceTypes.AuctionRelistRecommended,
            auctionId.Value,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Auction {AuctionId} closed after non-payment by buyer {BuyerId}; seller notified to relist.",
            auctionId.Value,
            defaultingUserId);
    }

    private async Task<AuctionRegistrationDeposit?> ForfeitWinnerDepositAsync(
        int auctionId,
        int userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var deposit = await _dbContext.AuctionRegistrationDeposits
            .FirstOrDefaultAsync(item =>
                    item.AuctionId == auctionId &&
                    item.UserId == userId &&
                    item.Status == AuctionRegistrationDepositStatuses.Paid,
                cancellationToken);

        if (deposit is null)
        {
            return null;
        }

        deposit.Status = AuctionRegistrationDepositStatuses.Forfeited;
        deposit.ForfeitedAt = now;
        deposit.UpdatedAt = now;
        return deposit;
    }

    private async Task LogAsync(
        int auctionId,
        int cancelledOrderId,
        int defaultingUserId,
        string action,
        string details,
        long? forfeitedDepositId = null,
        decimal? forfeitedAmount = null,
        int? secondChanceUserId = null,
        int? secondChanceOrderId = null,
        CancellationToken cancellationToken = default)
    {
        _dbContext.WinnerNonPaymentLogs.Add(new WinnerNonPaymentLog
        {
            AuctionId = auctionId,
            CancelledOrderId = cancelledOrderId,
            DefaultingUserId = defaultingUserId,
            ForfeitedDepositId = forfeitedDepositId,
            ForfeitedAmount = forfeitedAmount,
            Action = action,
            Details = details,
            SecondChanceUserId = secondChanceUserId,
            SecondChanceOrderId = secondChanceOrderId,
            CreatedAt = DateTime.UtcNow
        });

        await Task.CompletedTask;
    }
}
