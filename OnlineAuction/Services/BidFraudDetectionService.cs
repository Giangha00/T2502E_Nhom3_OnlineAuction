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
    private readonly IBidShadowBanService _shadowBanService;
    private readonly IBidChallengeService _challengeService;
    private readonly ILogger<BidFraudDetectionService> _logger;

    public BidFraudDetectionService(
        AuctionHouseDbContext dbContext,
        IOptions<BidFraudDetectionSettings> settings,
        IBidFraudAlertWriter alertWriter,
        IBidShadowBanService shadowBanService,
        IBidChallengeService challengeService,
        ILogger<BidFraudDetectionService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _alertWriter = alertWriter;
        _shadowBanService = shadowBanService;
        _challengeService = challengeService;
        _logger = logger;
    }

    public async Task<BidFraudGateResult> EvaluatePreBidAsync(
        int auctionId,
        int bidderId,
        decimal amount,
        decimal previousPrice,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return new BidFraudGateResult(true);
        }

        if (await _shadowBanService.IsShadowBannedAsync(bidderId, cancellationToken))
        {
            return new BidFraudGateResult(
                false,
                "Your bidding activity is temporarily restricted. Please try again later.");
        }

        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(item => item.Product)
            .ThenInclude(product => product.Seller)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        var bidder = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == bidderId, cancellationToken);

        if (auction is null || bidder is null)
        {
            return new BidFraudGateResult(true);
        }

        BidFraudGateResult? block = null;

        block ??= await EvaluateSellerRelatedBidderAsync(
            auction, bidder, bidId: null, ipAddress, applyEnforcement: true, cancellationToken);

        block ??= await EvaluateSameIpMultipleAccountsProjectedAsync(
            auctionId, bidderId, bidId: null, ipAddress, applyEnforcement: true, cancellationToken);

        block ??= await EvaluateRapidBiddingProjectedAsync(
            auctionId, bidderId, bidId: null, applyEnforcement: true, cancellationToken);

        block ??= await EvaluateAbnormalPriceJumpProjectedAsync(
            auctionId, bidderId, bidId: null, amount, previousPrice, applyEnforcement: true, cancellationToken);

        block ??= await EvaluateNewAccountHighBidProjectedAsync(
            auction, bidder, bidId: null, amount, applyEnforcement: true, cancellationToken);

        return block ?? new BidFraudGateResult(true);
    }

    public async Task EvaluatePostBidAsync(
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

        // Post-bid: alert + optional shadow-ban / challenge. Reject is handled pre-bid.
        await EvaluateSellerRelatedBidderAsync(
            bid.Auction, bid.Bidder, bid.Id, bid.IpAddress, applyEnforcement: false, cancellationToken);
        await EvaluateSameIpMultipleAccountsProjectedAsync(
            bid.AuctionId, bid.BidderId, bid.Id, bid.IpAddress, applyEnforcement: false, cancellationToken);
        await EvaluateRapidBiddingProjectedAsync(
            bid.AuctionId, bid.BidderId, bid.Id, applyEnforcement: false, cancellationToken);
        await EvaluateCollusionRoundTripAsync(bid, cancellationToken);
        await EvaluateAbnormalPriceJumpProjectedAsync(
            bid.AuctionId, bid.BidderId, bid.Id, bid.Amount, previousPrice, applyEnforcement: false, cancellationToken);
        await EvaluateNewAccountHighBidProjectedAsync(
            bid.Auction, bid.Bidder, bid.Id, bid.Amount, applyEnforcement: false, cancellationToken);
    }

    private async Task<BidFraudGateResult?> EvaluateSellerRelatedBidderAsync(
        Auction auction,
        ApplicationUser bidder,
        long? bidId,
        string? ipAddress,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        var seller = auction.Product.Seller;
        if (seller is null || seller.Id == bidder.Id)
        {
            return null;
        }

        var reasons = new List<string>();
        var sellerPhone = NormalizePhone(seller.PhoneNumber);
        var bidderPhone = NormalizePhone(bidder.PhoneNumber);
        if (!string.IsNullOrEmpty(sellerPhone)
            && !string.IsNullOrEmpty(bidderPhone)
            && sellerPhone == bidderPhone)
        {
            reasons.Add("matching_phone");
        }

        if (!string.IsNullOrWhiteSpace(seller.NormalizedEmail)
            && !string.IsNullOrWhiteSpace(bidder.NormalizedEmail)
            && string.Equals(seller.NormalizedEmail, bidder.NormalizedEmail, StringComparison.Ordinal))
        {
            reasons.Add("matching_email");
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var sellerUsedSameIp = await _dbContext.Bids
                .AsNoTracking()
                .AnyAsync(
                    item => item.BidderId == seller.Id && item.IpAddress == ipAddress,
                    cancellationToken);
            if (sellerUsedSameIp)
            {
                reasons.Add("seller_ip_reuse");
            }
        }

        if (reasons.Count == 0)
        {
            return null;
        }

        var message =
            $"Bidder #{bidder.Id} appears related to seller #{seller.Id} on auction #{auction.Id} ({string.Join(", ", reasons)}).";

        return await RaiseRuleAsync(
            auction.Id,
            bidId,
            bidder.Id,
            FraudAlertTypes.SellerRelatedBidder,
            FraudAlertSeverities.High,
            message,
            new { sellerId = seller.Id, bidderId = bidder.Id, reasons, ip = ipAddress },
            "Seller-related bidder",
            applyEnforcement,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> EvaluateSameIpMultipleAccountsProjectedAsync(
        int auctionId,
        int bidderId,
        long? bidId,
        string? ipAddress,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var userIds = await _dbContext.Bids
            .AsNoTracking()
            .Where(item => item.AuctionId == auctionId && item.IpAddress == ipAddress)
            .Select(item => item.BidderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!userIds.Contains(bidderId))
        {
            userIds.Add(bidderId);
        }

        if (userIds.Count < _settings.SameIpAccountThreshold)
        {
            return null;
        }

        var message =
            $"Multiple accounts ({userIds.Count}) bidding from same IP {ipAddress} on auction #{auctionId}.";

        return await RaiseRuleAsync(
            auctionId,
            bidId,
            bidderId,
            FraudAlertTypes.SameIpMultipleAccounts,
            FraudAlertSeverities.High,
            message,
            new { ip = ipAddress, userIds },
            "Multiple accounts from same IP",
            applyEnforcement,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> EvaluateRapidBiddingProjectedAsync(
        int auctionId,
        int bidderId,
        long? bidId,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_settings.RapidBidWindowSeconds);
        var count = await _dbContext.Bids
            .AsNoTracking()
            .CountAsync(item =>
                    item.AuctionId == auctionId
                    && item.BidderId == bidderId
                    && item.PlacedAt >= cutoff,
                cancellationToken);

        // Pre-bid: include the bid about to be placed.
        if (bidId is null)
        {
            count++;
        }

        if (count < _settings.RapidBidCountThreshold)
        {
            return null;
        }

        var message =
            $"User #{bidderId} placed {count} bids in {_settings.RapidBidWindowSeconds} seconds on auction #{auctionId}.";

        return await RaiseRuleAsync(
            auctionId,
            bidId,
            bidderId,
            FraudAlertTypes.RapidBidding,
            FraudAlertSeverities.Medium,
            message,
            new { bidCount = count, windowSeconds = _settings.RapidBidWindowSeconds },
            "Rapid repeated bidding",
            applyEnforcement,
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
        var message =
            $"Possible collusion between user #{firstUserId} and #{secondUserId} on auction #{bid.AuctionId}.";

        await RaiseRuleAsync(
            bid.AuctionId,
            bid.Id,
            bid.BidderId,
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
            applyEnforcement: false,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> EvaluateAbnormalPriceJumpProjectedAsync(
        int auctionId,
        int bidderId,
        long? bidId,
        decimal amount,
        decimal previousPrice,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        if (previousPrice <= 0)
        {
            return null;
        }

        var jumpPercent = (amount - previousPrice) / previousPrice * 100m;
        if (jumpPercent < _settings.AbnormalJumpPercent)
        {
            return null;
        }

        var message =
            $"Bid ${amount:N2} is {jumpPercent:N0}% above previous price ${previousPrice:N2} on auction #{auctionId}.";

        return await RaiseRuleAsync(
            auctionId,
            bidId,
            bidderId,
            FraudAlertTypes.AbnormalPriceJump,
            FraudAlertSeverities.Medium,
            message,
            new { previousPrice, amount, jumpPercent },
            "Abnormal price jump",
            applyEnforcement,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> EvaluateNewAccountHighBidProjectedAsync(
        Auction auction,
        ApplicationUser bidder,
        long? bidId,
        decimal amount,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        var accountAge = DateTime.UtcNow - bidder.CreatedAt;
        if (accountAge.TotalHours > _settings.NewAccountHoursThreshold)
        {
            return null;
        }

        var isHighBid = amount >= auction.StartingPrice * 2;
        if (!isHighBid && bidId.HasValue)
        {
            var topBids = await _dbContext.Bids
                .AsNoTracking()
                .Where(item => item.AuctionId == auction.Id)
                .OrderByDescending(item => item.Amount)
                .Take(3)
                .Select(item => new { item.Id, item.Amount })
                .ToListAsync(cancellationToken);

            isHighBid = topBids.Count == 3
                && topBids.Any(item => item.Id == bidId.Value)
                && amount >= auction.StartingPrice * 1.5m;
        }
        else if (!isHighBid)
        {
            isHighBid = amount >= auction.StartingPrice * 1.5m;
        }

        if (!isHighBid)
        {
            return null;
        }

        var message =
            $"New account (created {accountAge.TotalHours:N0}h ago) placed high bid ${amount:N2} on auction #{auction.Id}.";

        return await RaiseRuleAsync(
            auction.Id,
            bidId,
            bidder.Id,
            FraudAlertTypes.NewAccountHighBid,
            FraudAlertSeverities.Low,
            message,
            new { accountAgeHours = accountAge.TotalHours, amount, auction.StartingPrice },
            "New account high bid",
            applyEnforcement,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> RaiseRuleAsync(
        int auctionId,
        long? bidId,
        int bidderId,
        string alertType,
        string severity,
        string message,
        object metadata,
        string flagReason,
        bool applyEnforcement,
        CancellationToken cancellationToken)
    {
        var created = await _alertWriter.CreateAlertAsync(
            auctionId,
            bidId,
            bidderId,
            alertType,
            severity,
            message,
            JsonSerializer.Serialize(metadata),
            bidId.HasValue ? flagReason : null,
            cancellationToken);

        if (created)
        {
            _logger.LogWarning(
                "Fraud rule {AlertType} triggered for auction {AuctionId}, bid {BidId}, user {UserId}, severity {Severity}.",
                alertType,
                auctionId,
                bidId,
                bidderId,
                severity);

            await _challengeService.RequireChallengeAsync(
                bidderId,
                $"fraud:{alertType}",
                cancellationToken);
        }

        if (!IsHighSeverity(severity))
        {
            return null;
        }

        return await ApplyHighSeverityActionAsync(
            bidderId,
            alertType,
            severity,
            message,
            blockCurrentBid: applyEnforcement,
            cancellationToken);
    }

    private async Task<BidFraudGateResult?> ApplyHighSeverityActionAsync(
        int bidderId,
        string alertType,
        string severity,
        string message,
        bool blockCurrentBid,
        CancellationToken cancellationToken)
    {
        var action = _settings.HighSeverityAction?.Trim() ?? HighSeverityBidActions.ShadowBan;

        if (string.Equals(action, HighSeverityBidActions.Alert, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var appliedShadowBan = false;
        if (string.Equals(action, HighSeverityBidActions.ShadowBan, StringComparison.OrdinalIgnoreCase)
            || (!blockCurrentBid
                && string.Equals(action, HighSeverityBidActions.Reject, StringComparison.OrdinalIgnoreCase)))
        {
            // Post-bid Reject falls back to shadow-ban (bid already committed).
            var duration = TimeSpan.FromMinutes(Math.Max(1, _settings.ShadowBanDurationMinutes));
            await _shadowBanService.ApplyShadowBanAsync(bidderId, duration, message, cancellationToken);
            appliedShadowBan = true;
        }

        if (!blockCurrentBid)
        {
            return null;
        }

        if (string.Equals(action, HighSeverityBidActions.Reject, StringComparison.OrdinalIgnoreCase))
        {
            return new BidFraudGateResult(
                false,
                "Your bid was rejected by fraud protection.",
                AppliedShadowBan: appliedShadowBan,
                TriggeredAlertType: alertType,
                TriggeredSeverity: severity);
        }

        return new BidFraudGateResult(
            false,
            "Your bidding activity is temporarily restricted. Please try again later.",
            AppliedShadowBan: appliedShadowBan,
            TriggeredAlertType: alertType,
            TriggeredSeverity: severity);
    }

    private static bool IsHighSeverity(string severity) =>
        string.Equals(severity, FraudAlertSeverities.High, StringComparison.OrdinalIgnoreCase);

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
