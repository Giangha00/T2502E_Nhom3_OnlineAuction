using Microsoft.AspNetCore.SignalR;
using OnlineAuction.Helpers;
using OnlineAuction.Hubs;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class RealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<AppHub> _hubContext;
    private readonly ILogger<RealtimePublisher> _logger;

    public RealtimePublisher(
        IHubContext<AppHub> hubContext,
        ILogger<RealtimePublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(
        int userId,
        NotificationItemViewModel notification,
        int unreadCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group(AppHub.UserGroup(userId))
                .SendAsync(
                    "NotificationReceived",
                    new { notification, unreadCount },
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR notification push failed for user {UserId}.", userId);
        }
    }

    public async Task SendOrderCountToUserAsync(
        int userId,
        int orderCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group(AppHub.UserGroup(userId))
                .SendAsync("OrderCountUpdated", new { orderCount }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR order count push failed for user {UserId}.", userId);
        }
    }

    public async Task SendBidUpdateAsync(
        int auctionId,
        AuctionBidStateViewModel state,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                auctionId,
                currentPrice = state.CurrentPrice,
                bidCount = state.BidCount,
                minNextBid = state.MinNextBid,
                endDate = DateTimeUtilities.ToUtcIsoString(state.EndDate),
                isEnded = state.IsEnded,
                bidHistory = state.BidHistory.Select(bid => new
                {
                    bidderName = bid.BidderName,
                    amount = bid.Amount,
                    bidTime = bid.BidTime,
                    isWinning = bid.IsWinning,
                    status = bid.Status
                })
            };

            await _hubContext.Clients
                .Group(AppHub.AuctionGroup(auctionId))
                .SendAsync("BidUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR bid update failed for auction {AuctionId}.", auctionId);
        }
    }
}
