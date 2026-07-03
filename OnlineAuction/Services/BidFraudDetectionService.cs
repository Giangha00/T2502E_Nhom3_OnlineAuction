using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class BidFraudDetectionService : IBidFraudDetectionService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly BidFraudDetectionSettings _settings;
    private readonly IBidFraudAlertWriter _alertWriter;
    private readonly ILogger<BidFraudDetectionService> _logger;

    public BidFraudDetectionService(
        AuctionHouseDbContext dbContext,
        IOptions<BidFraudDetectionSettings> settings,
        IBidFraudAlertWriter alertWriter,
        ILogger<BidFraudDetectionService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _alertWriter = alertWriter;
        _logger = logger;
    }

    public async Task EvaluateAsync(
        int auctionId,
        long bidId,
        int bidderId,
        decimal previousPrice,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var bid = await _dbContext.Bids
            .AsNoTracking()
            .Include(item => item.Bidder)
            .Include(item => item.Auction)
            .ThenInclude(auction => auction.Product)
            .ThenInclude(product => product.Seller)
            .FirstOrDefaultAsync(item => item.Id == bidId, cancellationToken);

        if (bid is null)
        {
            return;
        }

        await EvaluateSameIpMultipleAccountsAsync(bid, cancellationToken);
        await EvaluateRapidBiddingAsync(bid, cancellationToken);
        await EvaluateCollusionRoundTripAsync(bid, cancellationToken);
        await EvaluateAbnormalPriceJumpAsync(bid, previousPrice, cancellationToken);
        await EvaluateNewAccountHighBidAsync(bid, cancellationToken);
    }

    private async Task EvaluateSameIpMultipleAccountsAsync(Bid bid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bid.IpAddress))
        {
            return;
        }

        var userIds = await _dbContext.Bids
            .AsNoTracking()
            .Where(item => item.AuctionId == bid.AuctionId && item.IpAddress == bid.IpAddress)
            .Select(item => item.BidderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count < _settings.SameIpAccountThreshold)
        {
            return;
        }

        var message = $"Multiple accounts ({userIds.Count}) bidding from same IP {bid.IpAddress} on auction #{bid.AuctionId}.";
        await CreateRuleAlertAsync(
            bid,
            FraudAlertTypes.SameIpMultipleAccounts,
            FraudAlertSeverities.High,
            message,
            new { ip = bid.IpAddress, userIds },
            "Multiple accounts from same IP",
            cancellationToken);
    }

    private async Task EvaluateRapidBiddingAsync(Bid bid, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_settings.RapidBidWindowSeconds);
        var count = await _dbContext.Bids
            .AsNoTracking()
            .CountAsync(item =>
                item.AuctionId == bid.AuctionId
                && item.BidderId == bid.BidderId
                && item.PlacedAt >= cutoff,
                cancellationToken);

        if (count < _settings.RapidBidCountThreshold)
        {
            return;
        }

        var message = $"User #{bid.BidderId} placed {count} bids in {_settings.RapidBidWindowSeconds} seconds on auction #{bid.AuctionId}.";
        await CreateRuleAlertAsync(
            bid,
            FraudAlertTypes.RapidBidding,
            FraudAlertSeverities.Medium,
            message,
            new { bidCount = count, windowSeconds = _settings.RapidBidWindowSeconds },
            "Rapid repeated bidding",
            cancellationToken);
    }

    private async Task EvaluateCollusionRoundTripAsync(Bid bid, CancellationToken cancellationToken)
    {
        var recentBidderIds = await _dbContext.Bids
            .AsNoTracking()
            .Where(item => item.AuctionId == bid.AuctionId)
            .OrderByDescending(item => item.PlacedAt)
            .Take(10)
            .Select(item => item.BidderId)
            .ToListAsync(cancellationToken);

        if (recentBidderIds.Count < _settings.CollusionRoundTripThreshold * 2)
        {
            return;
        }

        recentBidderIds.Reverse();
        var dominantUsers = recentBidderIds
            .GroupBy(userId => userId)
            .OrderByDescending(group => group.Count())
            .Take(2)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToList();

        if (dominantUsers.Count != 2)
        {
            return;
        }

        var dominantBidCount = dominantUsers.Sum(item => item.Count);
        var dominantShare = dominantBidCount / (decimal)recentBidderIds.Count;
        var alternatingTransitions = 0;

        for (var index = 1; index < recentBidderIds.Count; index++)
        {
            if (recentBidderIds[index] != recentBidderIds[index - 1]
                && dominantUsers.Any(user => user.UserId == recentBidderIds[index])
                && dominantUsers.Any(user => user.UserId == recentBidderIds[index - 1]))
            {
                alternatingTransitions++;
            }
        }

        var roundTrips = alternatingTransitions / 2;
        if (roundTrips < _settings.CollusionRoundTripThreshold && dominantShare < 0.8m)
        {
            return;
        }

        var firstUserId = dominantUsers[0].UserId;
        var secondUserId = dominantUsers[1].UserId;
        var message = $"Possible collusion between user #{firstUserId} and #{secondUserId} on auction #{bid.AuctionId}.";
        await CreateRuleAlertAsync(
            bid,
            FraudAlertTypes.CollusionRoundTrip,
            FraudAlertSeverities.High,
            message,
            new
            {
                userIds = new[] { firstUserId, secondUserId },
                recentBidderIds,
                roundTrips,
                dominantShare
            },
            "Possible collusion pattern",
            cancellationToken);
    }

    private async Task EvaluateAbnormalPriceJumpAsync(Bid bid, decimal previousPrice, CancellationToken cancellationToken)
    {
        if (previousPrice <= 0)
        {
            return;
        }

        var jumpPercent = (bid.Amount - previousPrice) / previousPrice * 100m;
        if (jumpPercent < _settings.AbnormalJumpPercent)
        {
            return;
        }

        var message = $"Bid ${bid.Amount:N2} is {jumpPercent:N0}% above previous price ${previousPrice:N2} on auction #{bid.AuctionId}.";
        await CreateRuleAlertAsync(
            bid,
            FraudAlertTypes.AbnormalPriceJump,
            FraudAlertSeverities.Medium,
            message,
            new { previousPrice, bid.Amount, jumpPercent },
            "Abnormal price jump",
            cancellationToken);
    }

    private async Task EvaluateNewAccountHighBidAsync(Bid bid, CancellationToken cancellationToken)
    {
        var accountAge = DateTime.UtcNow - bid.Bidder.CreatedAt;
        if (accountAge.TotalHours > _settings.NewAccountHoursThreshold)
        {
            return;
        }

        var isHighBid = bid.Amount >= bid.Auction.StartingPrice * 2;
        if (!isHighBid)
        {
            var topBids = await _dbContext.Bids
                .AsNoTracking()
                .Where(item => item.AuctionId == bid.AuctionId)
                .OrderByDescending(item => item.Amount)
                .Take(3)
                .Select(item => new { item.Id, item.Amount })
                .ToListAsync(cancellationToken);

            isHighBid = topBids.Count == 3
                && topBids.Any(item => item.Id == bid.Id)
                && bid.Amount >= bid.Auction.StartingPrice * 1.5m;
        }

        if (!isHighBid)
        {
            return;
        }

        var message = $"New account (created {accountAge.TotalHours:N0}h ago) placed high bid ${bid.Amount:N2} on auction #{bid.AuctionId}.";
        await CreateRuleAlertAsync(
            bid,
            FraudAlertTypes.NewAccountHighBid,
            FraudAlertSeverities.Low,
            message,
            new { accountAgeHours = accountAge.TotalHours, bid.Amount, bid.Auction.StartingPrice },
            "New account high bid",
            cancellationToken);
    }

    private async Task CreateRuleAlertAsync(
        Bid bid,
        string alertType,
        string severity,
        string message,
        object metadata,
        string flagReason,
        CancellationToken cancellationToken)
    {
        var created = await _alertWriter.CreateAlertAsync(
            bid.AuctionId,
            bid.Id,
            bid.BidderId,
            alertType,
            severity,
            message,
            JsonSerializer.Serialize(metadata),
            flagReason,
            cancellationToken);

        if (created)
        {
            _logger.LogWarning(
                "Fraud rule {AlertType} triggered for auction {AuctionId}, bid {BidId}, user {UserId}.",
                alertType,
                bid.AuctionId,
                bid.Id,
                bid.BidderId);
        }
    }
}
