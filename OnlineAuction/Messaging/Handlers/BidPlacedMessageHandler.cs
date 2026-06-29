using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Messaging.Handlers;

public interface IBidPlacedMessageHandler
{
    Task HandleAsync(BidPlacedMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Verifies bidder id and amount against the live auction state before broadcasting updates.
/// Skips stale messages when a newer bid has already been recorded.
/// </summary>
public sealed class BidPlacedMessageHandler : IBidPlacedMessageHandler
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IBidService _bidService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly INotificationDeliveryService _notificationDelivery;
    private readonly ILogger<BidPlacedMessageHandler> _logger;

    public BidPlacedMessageHandler(
        AuctionHouseDbContext dbContext,
        IBidService bidService,
        IRealtimePublisher realtimePublisher,
        INotificationDeliveryService notificationDelivery,
        ILogger<BidPlacedMessageHandler> logger)
    {
        _dbContext = dbContext;
        _bidService = bidService;
        _realtimePublisher = realtimePublisher;
        _notificationDelivery = notificationDelivery;
        _logger = logger;
    }

    public async Task HandleAsync(BidPlacedMessage message, CancellationToken cancellationToken = default)
    {
        var winningBid = await _dbContext.Bids
            .AsNoTracking()
            .Where(b => b.AuctionId == message.AuctionId && b.IsWinning)
            .OrderByDescending(b => b.Amount)
            .ThenByDescending(b => b.PlacedAt)
            .Select(b => new { b.Id, b.BidderId, b.Amount })
            .FirstOrDefaultAsync(cancellationToken);

        if (winningBid is null)
        {
            _logger.LogDebug(
                "Skipping bid message for auction {AuctionId}: no winning bid in database.",
                message.AuctionId);
            return;
        }

        if (winningBid.BidderId != message.BidderId || winningBid.Amount != message.Amount)
        {
            _logger.LogDebug(
                "Skipping stale bid message for auction {AuctionId}. Expected bidder {ExpectedBidder} amount {ExpectedAmount}, got bidder {ActualBidder} amount {ActualAmount}.",
                message.AuctionId,
                winningBid.BidderId,
                winningBid.Amount,
                message.BidderId,
                message.Amount);
            return;
        }

        if (winningBid.Id != message.BidId)
        {
            _logger.LogDebug(
                "Skipping stale bid message for auction {AuctionId}: bid id {MessageBidId} is not current winning bid {WinningBidId}.",
                message.AuctionId,
                message.BidId,
                winningBid.Id);
            return;
        }

        var bidState = await _bidService.GetBidStateAsync(message.AuctionId, cancellationToken);
        if (bidState is null)
        {
            return;
        }

        await _realtimePublisher.SendBidUpdateAsync(message.AuctionId, bidState, cancellationToken);

        foreach (var outbidUserId in message.OutbidUserIds)
        {
            if (outbidUserId <= 0 || outbidUserId == message.BidderId)
            {
                continue;
            }

            await _notificationDelivery.DeliverOutbidAsync(
                outbidUserId,
                message.ProductName,
                message.AuctionId,
                cancellationToken);
        }
    }
}
