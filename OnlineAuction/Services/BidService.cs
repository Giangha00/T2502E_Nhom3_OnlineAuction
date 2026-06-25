using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class BidService : IBidService
{
    private const int AntiSnipeThresholdMinutes = 5;
    private const int AntiSnipeExtensionMinutes = 5;
    private const int BidHistoryLimit = 20;

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IAuctionRegistrationService _registrationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BidService> _logger;

    public BidService(
        AuctionHouseDbContext dbContext,
        IAuctionRegistrationService registrationService,
        INotificationService notificationService,
        ILogger<BidService> logger)
    {
        _dbContext = dbContext;
        _registrationService = registrationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PlaceBidResult> PlaceBidAsync(int auctionId, int bidderId, decimal amount)
    {
        if (auctionId <= 0 || amount <= 0)
        {
            return Fail("Invalid bid.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => PlaceBidCoreAsync(auctionId, bidderId, amount));
    }

    private async Task<PlaceBidResult> PlaceBidCoreAsync(int auctionId, int bidderId, decimal amount)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId);

        if (auction is null)
        {
            _logger.LogWarning("Bid rejected: auction {AuctionId} not found.", auctionId);
            return Fail("Auction not found.", 404);
        }

        var bidder = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == bidderId);

        if (bidder is null)
        {
            return Fail("Please sign in to place a bid.", 401);
        }

        if (bidder.Status != UserStatus.Active)
        {
            _logger.LogWarning("Bid rejected: inactive user {UserId}.", bidderId);
            return Fail("Your account is not active.");
        }

        var validationError = ValidateBid(auction, bidderId, amount);
        if (validationError is not null)
        {
            _logger.LogWarning(
                "Bid rejected for auction {AuctionId} by user {UserId}: {Reason}",
                auctionId,
                bidderId,
                validationError);
            return Fail(validationError);
        }

        var registrationError = await _registrationService.GetBidBlockMessageAsync(
            auctionId,
            bidderId,
            auction.RequiresRegistration);

        if (registrationError is not null)
        {
            _logger.LogWarning(
                "Bid rejected for auction {AuctionId} by user {UserId}: {Reason}",
                auctionId,
                bidderId,
                registrationError);
            return Fail(registrationError);
        }

        var previousWinningBids = await _dbContext.Bids
            .Where(b => b.AuctionId == auctionId && b.IsWinning)
            .ToListAsync();

        var outbidUserIds = previousWinningBids
            .Where(b => b.BidderId != bidderId)
            .Select(b => b.BidderId)
            .Distinct()
            .ToList();

        foreach (var previousBid in previousWinningBids)
        {
            previousBid.IsWinning = false;
        }

        var placedAt = DateTime.UtcNow;
        _dbContext.Bids.Add(new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            BidType = BidTypes.Manual,
            IsWinning = true,
            PlacedAt = placedAt,
            CreatedAt = placedAt
        });

        auction.CurrentPrice = amount;
        auction.UpdatedAt = placedAt;

        if (DateTimeUtilities.RemainingUtc(auction.EndDate).TotalMinutes < AntiSnipeThresholdMinutes)
        {
            // auction.EndDate = DateTimeUtilities.AsUtc(auction.EndDate).AddMinutes(AntiSnipeExtensionMinutes);
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var productName = auction.Product.Name;
        foreach (var outbidUserId in outbidUserIds)
        {
            await _notificationService.CreateAndPushAsync(
                outbidUserId,
                "You've been outbid",
                $"Someone placed a higher bid on {productName}.",
                NotificationType.Auction,
                $"/Auction/Detail/{auctionId}",
                NotificationReferenceTypes.AuctionOutbid,
                auctionId,
                TimeSpan.FromMinutes(5));
        }

        var bidCount = await _dbContext.Bids.CountAsync(b => b.AuctionId == auctionId);
        var bidHistory = await LoadBidHistoryAsync(auctionId);

        return PlaceBidResult.Ok(
            "Bid placed successfully.",
            auction.CurrentPrice,
            bidCount,
            auction.CurrentPrice + auction.BidStep,
            auction.EndDate,
            bidHistory);
    }

    private static string? ValidateBid(Auction auction, int bidderId, decimal amount)
    {
        if (auction.Product.SellerId == bidderId)
        {
            return "You cannot bid on your own listing.";
        }

        if (auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon))
        {
            return auction.Status switch
            {
                AuctionStatuses.PendingReview => "This auction is pending review and not yet open for bidding.",
                AuctionStatuses.Rejected => "This auction listing was rejected.",
                AuctionStatuses.Scheduled => "This auction has not started yet.",
                AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment => "This auction has ended.",
                AuctionStatuses.Cancelled => "This auction has been cancelled.",
                AuctionStatuses.Completed => "This auction is completed.",
                _ => "This auction is not accepting bids."
            };
        }

        if (!DateTimeUtilities.IsInFutureUtc(auction.EndDate))
        {
            return "This auction has ended.";
        }

        var minBid = auction.CurrentPrice + auction.BidStep;
        if (amount < minBid)
        {
            return $"Your bid must be at least ${minBid:N0}.";
        }

        if (!IsValidBidIncrement(auction.CurrentPrice, auction.BidStep, amount))
        {
            return $"Your bid must increase by at least ${auction.BidStep:N0} per step.";
        }

        return null;
    }

    private static bool IsValidBidIncrement(decimal currentPrice, decimal bidStep, decimal amount)
    {
        if (bidStep <= 0)
        {
            return false;
        }

        var increment = amount - currentPrice;
        if (increment < bidStep)
        {
            return false;
        }

        var steps = increment / bidStep;
        return steps == decimal.Truncate(steps);
    }

    private async Task<IReadOnlyList<BidHistoryItemViewModel>> LoadBidHistoryAsync(int auctionId)
    {
        var bids = await _dbContext.Bids
            .AsNoTracking()
            .Include(b => b.Bidder)
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.PlacedAt)
            .Take(BidHistoryLimit)
            .ToListAsync();

        return ProductDetailMapper.MapBidHistory(bids);
    }

    private static PlaceBidResult Fail(string message, int statusCode = 400) =>
        PlaceBidResult.Fail(message, statusCode);
}
