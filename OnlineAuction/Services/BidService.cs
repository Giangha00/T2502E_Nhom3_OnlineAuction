using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Messaging;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Messaging.Handlers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class BidService : IBidService
{
    private const int BidHistoryPreviewLimit = 10;
    public const int BidHistoryPageSize = 25;

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IAuctionRegistrationService _registrationService;
    private readonly IBidFraudDetectionService _fraudDetectionService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BidFraudDetectionSettings _fraudSettings;
    private readonly ILogger<BidService> _logger;

    public BidService(
        AuctionHouseDbContext dbContext,
        IAuctionRegistrationService registrationService,
        IBidFraudDetectionService fraudDetectionService,
        IRabbitMqPublisher publisher,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BidFraudDetectionSettings> fraudSettings,
        ILogger<BidService> logger)
    {
        _dbContext = dbContext;
        _registrationService = registrationService;
        _fraudDetectionService = fraudDetectionService;
        _publisher = publisher;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _fraudSettings = fraudSettings.Value;
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

        var previousPrice = auction.CurrentPrice;
        var ipAddress = GetClientIpAddress();

        try
        {
            var fraudGate = await _fraudDetectionService.EvaluatePreBidAsync(
                auctionId,
                bidderId,
                amount,
                previousPrice,
                ipAddress);

            if (!fraudGate.IsAllowed)
            {
                _logger.LogWarning(
                    "Bid rejected by fraud gate for auction {AuctionId}, user {UserId}, alert {AlertType}, shadowBan={ShadowBan}.",
                    auctionId,
                    bidderId,
                    fraudGate.TriggeredAlertType,
                    fraudGate.AppliedShadowBan);
                return Fail(
                    fraudGate.BlockMessage ?? "Your bid was rejected by fraud protection.",
                    403);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Pre-bid fraud detection failed for auction {AuctionId}, user {UserId}. Allowing bid to proceed.",
                auctionId,
                bidderId);
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
        var newBid = new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            BidType = BidTypes.Manual,
            IsWinning = true,
            PlacedAt = placedAt,
            CreatedAt = placedAt,
            IpAddress = ipAddress,
            UserAgent = GetUserAgent()
        };

        _dbContext.Bids.Add(newBid);

        auction.CurrentPrice = amount;
        auction.UpdatedAt = placedAt;

        var remainingMinutes = DateTimeUtilities.RemainingUtc(auction.EndDate).TotalMinutes;
        var endDateUtc = DateTimeUtilities.AsUtc(auction.EndDate);
        var startDateUtc = DateTimeUtilities.AsUtc(auction.StartDate);
        var totalLiveWindowMinutes = Math.Max(0, (endDateUtc - startDateUtc).TotalMinutes);
        var extensionMinutesAlreadyApplied = Math.Max(0, totalLiveWindowMinutes - AuctionScheduleHelper.DefaultLiveDuration.TotalMinutes);
        var maxExtensionMinutes = Math.Max(0, _fraudSettings.MaxEndDateExtensionTotalMinutes);
        var maxExtensionCount = Math.Max(0, _fraudSettings.MaxAntiSnipeExtensions);
        var antiSnipeExtensionCount = Math.Max(0, (int)Math.Floor(extensionMinutesAlreadyApplied / Math.Max(1, _fraudSettings.AntiSnipeExtensionMinutes)));
        var withinExtensionCap = antiSnipeExtensionCount < maxExtensionCount && extensionMinutesAlreadyApplied < maxExtensionMinutes;

        if (remainingMinutes < _fraudSettings.AntiSnipeThresholdMinutes && withinExtensionCap)
        {
            var extendedEndDate = endDateUtc.AddMinutes(_fraudSettings.AntiSnipeExtensionMinutes);
            auction.EndDate = extendedEndDate;
            _logger.LogInformation(
                "Anti-snipe extended auction {AuctionId} to {NewEndDate}.",
                auction.Id,
                auction.EndDate);
        }
        else if (remainingMinutes < _fraudSettings.AntiSnipeThresholdMinutes)
        {
            _logger.LogInformation(
                "Anti-snipe extension skipped for auction {AuctionId} because the configured extension cap was reached (remainingMinutes={RemainingMinutes}, extensionCount={ExtensionCount}, maxExtensions={MaxExtensions}, extensionTotalMinutes={TotalExtensionMinutes}, maxTotalMinutes={MaxExtensionMinutes}).",
                auction.Id,
                remainingMinutes,
                antiSnipeExtensionCount,
                maxExtensionCount,
                extensionMinutesAlreadyApplied,
                maxExtensionMinutes);
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _fraudDetectionService.EvaluatePostBidAsync(auctionId, newBid.Id, bidderId, previousPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Fraud detection failed after successful bid {BidId} for auction {AuctionId}.",
                newBid.Id,
                auctionId);
        }

        var productName = auction.Product.Name;
        var bidPlacedMessage = new BidPlacedMessage
        {
            AuctionId = auctionId,
            BidId = newBid.Id,
            BidderId = bidderId,
            SellerId = auction.Product.SellerId,
            Amount = amount,
            PreviousPrice = previousPrice,
            OutbidUserIds = outbidUserIds,
            ProductName = productName
        };

        if (!_publisher.TryPublish(RabbitMqQueueNames.BidsPlaced, bidPlacedMessage))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IBidPlacedMessageHandler>();
                await handler.HandleAsync(bidPlacedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Inline bid side-effect handling failed for auction {AuctionId}.", auctionId);
            }
        }

        var bidCount = await _dbContext.Bids.CountAsync(b => b.AuctionId == auctionId);
        var bidHistory = await LoadBidHistoryAsync(
            auctionId,
            ProductDetailMapper.ShouldRevealBidderIdentity(auction),
            BidHistoryPreviewLimit);

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
                AuctionStatuses.Scheduled => "The live auction has not started yet.",
                AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment => "This auction has ended.",
                AuctionStatuses.Cancelled => "This auction has been cancelled.",
                AuctionStatuses.Completed => "This auction is completed.",
                _ => "This auction is not accepting bids."
            };
        }

        var now = DateTime.UtcNow;
        if (now < DateTimeUtilities.AsUtc(auction.StartDate))
        {
            return "The live auction has not started yet.";
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

    public async Task<AuctionBidStateViewModel?> GetBidStateAsync(
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            return null;
        }

        var bidCount = await _dbContext.Bids.CountAsync(b => b.AuctionId == auctionId, cancellationToken);
        var bidHistory = await LoadBidHistoryAsync(
            auctionId,
            ProductDetailMapper.ShouldRevealBidderIdentity(auction),
            BidHistoryPreviewLimit);
        return BuildBidState(auctionId, auction, bidCount, bidHistory);
    }

    public async Task<AuctionBidHistoryPageViewModel?> GetAuctionBidHistoryPageAsync(
        int auctionId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction?.Product is null)
        {
            return null;
        }

        var revealIdentity = ProductDetailMapper.ShouldRevealBidderIdentity(auction);
        var bidCount = await _dbContext.Bids.CountAsync(b => b.AuctionId == auctionId, cancellationToken);
        var totalPages = bidCount == 0
            ? 1
            : (int)Math.Ceiling(bidCount / (double)BidHistoryPageSize);

        if (page < 1)
        {
            page = 1;
        }
        else if (page > totalPages)
        {
            page = totalPages;
        }

        var skip = (page - 1) * BidHistoryPageSize;
        var bids = await LoadBidHistoryAsync(
            auctionId,
            revealIdentity,
            take: BidHistoryPageSize,
            skip: skip);

        return new AuctionBidHistoryPageViewModel
        {
            AuctionId = auction.Id,
            ProductName = auction.Product.Name,
            ProductImageUrl = auction.Product.PrimaryImage,
            CurrentPrice = auction.CurrentPrice,
            BidCount = bidCount,
            IsEnded = revealIdentity,
            Page = page,
            PageSize = BidHistoryPageSize,
            TotalPages = totalPages,
            Bids = bids
        };
    }

    private static AuctionBidStateViewModel BuildBidState(
        int auctionId,
        Auction auction,
        int bidCount,
        IReadOnlyList<BidHistoryItemViewModel> bidHistory)
    {
        var isEnded = ProductDetailMapper.ShouldRevealBidderIdentity(auction);

        return new AuctionBidStateViewModel
        {
            AuctionId = auctionId,
            CurrentPrice = auction.CurrentPrice,
            BidCount = bidCount,
            MinNextBid = auction.CurrentPrice + auction.BidStep,
            EndDate = auction.EndDate,
            IsEnded = isEnded,
            BidHistory = bidHistory
        };
    }

    private async Task<IReadOnlyList<BidHistoryItemViewModel>> LoadBidHistoryAsync(
        int auctionId,
        bool revealBidderIdentity,
        int? take,
        int skip = 0)
    {
        Bid? winningBid = null;
        if (skip > 0)
        {
            winningBid = await _dbContext.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Where(b => b.AuctionId == auctionId && b.IsWinning)
                .FirstOrDefaultAsync();
        }

        var query = _dbContext.Bids
            .AsNoTracking()
            .Include(b => b.Bidder)
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.PlacedAt);

        List<Bid> bids;
        if (take.HasValue)
        {
            bids = await query.Skip(skip).Take(take.Value).ToListAsync();
        }
        else if (skip > 0)
        {
            bids = await query.Skip(skip).ToListAsync();
        }
        else
        {
            bids = await query.ToListAsync();
        }

        return ProductDetailMapper.MapBidHistory(bids, revealBidderIdentity, winningBid: winningBid);
    }

    private static PlaceBidResult Fail(string message, int statusCode = 400) =>
        PlaceBidResult.Fail(message, statusCode);

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        return userAgent.Length <= 512 ? userAgent : userAgent[..512];
    }
}
