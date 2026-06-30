using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Data;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Messaging.Handlers;

public interface IAuctionLifecycleMessageHandler
{
    Task HandleAsync(AuctionLifecycleMessage message, CancellationToken cancellationToken = default);
}

public sealed class AuctionLifecycleMessageHandler : IAuctionLifecycleMessageHandler
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IOrderCreationService _orderCreationService;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly IAdminAuctionVerificationService _verificationService;
    private readonly IRegistrationDepositRefundService _depositRefundService;
    private readonly ILogger<AuctionLifecycleMessageHandler> _logger;

    public AuctionLifecycleMessageHandler(
        AuctionHouseDbContext dbContext,
        IOrderCreationService orderCreationService,
        IOrderService orderService,
        INotificationService notificationService,
        IAdminAuctionVerificationService verificationService,
        IRegistrationDepositRefundService depositRefundService,
        ILogger<AuctionLifecycleMessageHandler> logger)
    {
        _dbContext = dbContext;
        _orderCreationService = orderCreationService;
        _orderService = orderService;
        _notificationService = notificationService;
        _verificationService = verificationService;
        _depositRefundService = depositRefundService;
        _logger = logger;
    }

    public async Task HandleAsync(AuctionLifecycleMessage message, CancellationToken cancellationToken = default)
    {
        switch (message.Action)
        {
            case AuctionLifecycleAction.FinalizeExpiredAuctions:
                await _orderCreationService.FinalizeExpiredAuctionsAsync(cancellationToken);
                break;

            case AuctionLifecycleAction.CancelExpiredOrders:
                await _orderService.CancelAllExpiredPendingOrdersAsync();
                break;

            case AuctionLifecycleAction.ProcessEndingSoonNotifications:
                await _notificationService.ProcessAuctionEndingSoonNotificationsAsync(cancellationToken);
                break;

            case AuctionLifecycleAction.ProcessStartingSoonNotifications:
                await _notificationService.ProcessAuctionStartingSoonNotificationsAsync(cancellationToken);
                break;

            case AuctionLifecycleAction.ActivateScheduledAuctions:
                await _verificationService.ActivateScheduledAuctionsAsync(cancellationToken);
                break;

            case AuctionLifecycleAction.AuctionEnded when message.AuctionId.HasValue:
                await FinalizeSingleAuctionAsync(message.AuctionId.Value, cancellationToken);
                break;

            case AuctionLifecycleAction.AuctionEndingSoon when message.AuctionId.HasValue:
                await ProcessEndingSoonForAuctionAsync(message.AuctionId.Value, cancellationToken);
                break;
        }
    }

    private async Task FinalizeSingleAuctionAsync(int auctionId, CancellationToken cancellationToken)
    {
        var orderId = await _orderCreationService.CreatePendingPaymentOrderForAuctionAsync(auctionId, cancellationToken);
        if (orderId.HasValue)
        {
            _logger.LogInformation("Created pending payment order {OrderId} for auction {AuctionId}.", orderId, auctionId);
        }

        var refundedCount = await _depositRefundService.RefundLoserDepositsForAuctionAsync(auctionId, cancellationToken);
        if (refundedCount > 0)
        {
            _logger.LogInformation(
                "Refunded {RefundedCount} loser deposits for auction {AuctionId}.",
                refundedCount,
                auctionId);
        }
    }

    private async Task ProcessEndingSoonForAuctionAsync(int auctionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(60);

        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(
                a => a.Id == auctionId
                     && a.ListingType == ListingTypes.Auction
                     && (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon)
                     && a.DeletedAt == null
                     && a.EndDate > now
                     && a.EndDate <= threshold,
                cancellationToken);

        if (auction is null)
        {
            return;
        }

        var bidderIds = await _dbContext.Bids
            .AsNoTracking()
            .Where(b => b.AuctionId == auctionId)
            .Select(b => b.BidderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var watcherIds = await _dbContext.AuctionRegistrations
            .AsNoTracking()
            .Where(r => r.AuctionId == auctionId && r.Status == AuctionRegistrationStatuses.Approved)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken);

        var recipientIds = bidderIds.Concat(watcherIds).Distinct().ToList();
        var productName = auction.Product?.Name ?? "an auction";
        var relatedUrl = $"/Auction/Detail/{auction.Id}";

        foreach (var userId in recipientIds)
        {
            await _notificationService.CreateAndPushAsync(
                userId,
                "Auction ending soon",
                $"{productName} ends within the next hour.",
                NotificationType.Auction,
                relatedUrl,
                NotificationReferenceTypes.AuctionEndingSoon,
                auction.Id,
                cancellationToken: cancellationToken);
        }
    }
}
