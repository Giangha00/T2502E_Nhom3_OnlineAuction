using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class AuctionRegistrationService : IAuctionRegistrationService
{
    private static readonly HashSet<string> ActiveRegistrationStatuses =
    [
        AuctionRegistrationStatuses.Pending,
        AuctionRegistrationStatuses.Approved,
        AuctionRegistrationStatuses.Rejected
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IRegistrationDepositRefundService _depositRefundService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AuctionRegistrationService> _logger;

    public AuctionRegistrationService(
        AuctionHouseDbContext dbContext,
        IRegistrationDepositRefundService depositRefundService,
        INotificationService notificationService,
        ILogger<AuctionRegistrationService> logger)
    {
        _dbContext = dbContext;
        _depositRefundService = depositRefundService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<AuctionRegistrationResult> RegisterAsync(int auctionId, int userId)
    {
        if (auctionId <= 0 || userId <= 0)
        {
            return Fail("Invalid registration request.");
        }

        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId);

        if (auction is null)
        {
            return Fail("Auction not found.", 404);
        }

        var validationError = ValidateRegistrationWindow(auction);
        if (validationError is not null)
        {
            return Fail(validationError);
        }

        if (!auction.RequiresRegistration)
        {
            return Fail("This auction does not require registration. You can place a bid directly.");
        }

        if (auction.Product.SellerId == userId)
        {
            return Fail("You cannot register for your own auction.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return Fail("Please sign in to register.", 401);
        }

        var existing = await _dbContext.AuctionRegistrations
            .FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.UserId == userId);

        if (existing is not null && ActiveRegistrationStatuses.Contains(existing.Status))
        {
            return Fail(existing.Status switch
            {
                AuctionRegistrationStatuses.Pending => "You already have a pending registration for this auction.",
                AuctionRegistrationStatuses.Approved => "You are already registered for this auction.",
                AuctionRegistrationStatuses.Rejected => "Your registration was rejected.",
                _ => "You are already registered for this auction."
            });
        }

        var now = DateTime.UtcNow;
        var status = AuctionRegistrationStatuses.Approved;

        if (existing is not null && existing.Status == AuctionRegistrationStatuses.Cancelled)
        {
            existing.Status = status;
            existing.RegisteredAt = now;
            existing.ReviewedAt = now;
            existing.ReviewedBy = null;
            existing.RejectReason = null;
            existing.UpdatedAt = now;
        }
        else
        {
            _dbContext.AuctionRegistrations.Add(new AuctionRegistration
            {
                AuctionId = auctionId,
                UserId = userId,
                Status = status,
                RegisteredAt = now,
                ReviewedAt = now,
                CreatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync();

        var registrationCount = await CountApprovedRegistrationsAsync(auctionId);

        _logger.LogInformation(
            "User {UserId} registered for auction {AuctionId} with status {Status}.",
            userId,
            auctionId,
            status);

        return AuctionRegistrationResult.Ok(
            "Registration successful. You can now place bids.",
            status,
            registrationCount);
    }

    public async Task<AuctionRegistrationResult> CancelRegistrationAsync(int auctionId, int userId)
    {
        if (auctionId <= 0 || userId <= 0)
        {
            return Fail("Invalid request.");
        }

        var registration = await _dbContext.AuctionRegistrations
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.UserId == userId);

        if (registration is null ||
            registration.Status is not (AuctionRegistrationStatuses.Pending or AuctionRegistrationStatuses.Approved))
        {
            return Fail("No active registration found to cancel.");
        }

        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auctionId);

        if (auction is null)
        {
            return Fail("Auction not found.", 404);
        }

        if (!AuctionScheduleHelper.IsRegistrationOpen(auction))
        {
            return Fail("Registration is closed. You can no longer cancel your registration.");
        }

        var hasBid = await _dbContext.Bids
            .AnyAsync(b => b.AuctionId == auctionId && b.BidderId == userId);

        if (hasBid)
        {
            return Fail("You cannot cancel registration after placing a bid.");
        }

        var now = DateTime.UtcNow;
        decimal? refundedAmount = null;

        var paidDeposit = registration.Deposits
            .Where(d => d.Status == AuctionRegistrationDepositStatuses.Paid)
            .OrderByDescending(d => d.PaidAt)
            .FirstOrDefault();

        if (paidDeposit is not null)
        {
            var refundResult = await _depositRefundService.RefundDepositAsync(
                paidDeposit.Id,
                pushNotification: false);
            if (!refundResult.Success)
            {
                _logger.LogWarning(
                    "Cancel registration refund failed. AuctionId={AuctionId}, UserId={UserId}, DepositId={DepositId}, Message={Message}",
                    auctionId,
                    userId,
                    paidDeposit.Id,
                    refundResult.Message);

                return Fail(refundResult.Message, refundResult.StatusCode);
            }

            refundedAmount = refundResult.DepositAmount;
        }

        foreach (var pendingDeposit in registration.Deposits
                     .Where(d => d.Status == AuctionRegistrationDepositStatuses.Pending))
        {
            pendingDeposit.Status = AuctionRegistrationDepositStatuses.Cancelled;
            pendingDeposit.UpdatedAt = now;
        }

        registration.Status = AuctionRegistrationStatuses.Cancelled;
        registration.UpdatedAt = now;
        await _dbContext.SaveChangesAsync();

        var registrationCount = await CountApprovedRegistrationsAsync(auctionId);

        var message = refundedAmount.HasValue
            ? $"Registration cancelled. Your deposit of ${refundedAmount.Value:N0} has been refunded."
            : "Registration cancelled.";

        _logger.LogInformation(
            "User {UserId} cancelled registration for auction {AuctionId}. RefundedAmount={RefundedAmount}",
            userId,
            auctionId,
            refundedAmount);

        var auctionForNotification = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId);

        var productName = auctionForNotification?.Product?.Name ?? "the auction";
        var notificationMessage = refundedAmount.HasValue
            ? $"Your registration for {productName} was cancelled. Deposit of ${refundedAmount.Value:N0} has been refunded."
            : $"Your registration for {productName} was cancelled.";

        await _notificationService.CreateAndPushAsync(
            userId,
            "Registration cancelled",
            notificationMessage,
            NotificationType.Auction,
            $"/Auction/Detail/{auctionId}",
            cancellationToken: default);

        return AuctionRegistrationResult.Ok(
            message,
            AuctionRegistrationStatuses.Cancelled,
            registrationCount,
            refundedAmount);
    }

    public async Task<string?> GetBidBlockMessageAsync(int auctionId, int userId, bool requiresRegistration)
    {
        if (!requiresRegistration)
        {
            return null;
        }

        var registration = await _dbContext.AuctionRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.UserId == userId);

        if (registration is null ||
            registration.Status == AuctionRegistrationStatuses.Cancelled)
        {
            return "You must register for this auction before placing a bid.";
        }

        return registration.Status switch
        {
            AuctionRegistrationStatuses.Pending => "Your registration is pending approval.",
            AuctionRegistrationStatuses.Rejected => string.IsNullOrWhiteSpace(registration.RejectReason)
                ? "Your registration was rejected."
                : $"Your registration was rejected. {registration.RejectReason}",
            AuctionRegistrationStatuses.Approved => null,
            _ => "You must register for this auction before placing a bid."
        };
    }

    public static async Task<int> CountApprovedRegistrationsAsync(AuctionHouseDbContext dbContext, int auctionId) =>
        await dbContext.AuctionRegistrations
            .AsNoTracking()
            .CountAsync(r =>
                r.AuctionId == auctionId &&
                r.Status == AuctionRegistrationStatuses.Approved);

    private Task<int> CountApprovedRegistrationsAsync(int auctionId) =>
        CountApprovedRegistrationsAsync(_dbContext, auctionId);

    private static string? ValidateRegistrationWindow(Auction auction)
    {
        if (!auction.RequiresRegistration)
        {
            return "This auction does not require registration. You can place a bid directly.";
        }

        if (auction.Status is AuctionStatuses.PendingReview or AuctionStatuses.Rejected or AuctionStatuses.Cancelled)
        {
            return auction.Status switch
            {
                AuctionStatuses.PendingReview => "This auction is pending review.",
                AuctionStatuses.Rejected => "This auction listing was rejected.",
                AuctionStatuses.Cancelled => "This auction has been cancelled.",
                _ => "This auction is not open for registration."
            };
        }

        var now = DateTime.UtcNow;
        var registrationStart = DateTimeUtilities.AsUtc(auction.RegistrationStartDate);
        var registrationEnd = DateTimeUtilities.AsUtc(auction.RegistrationEndDate);

        if (now < registrationStart)
        {
            return "Registration for this auction has not opened yet.";
        }

        if (now >= registrationEnd)
        {
            return auction.Status is AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment or AuctionStatuses.Completed
                ? "This auction has ended."
                : "Registration for this auction is closed.";
        }

        if (!AuctionScheduleHelper.IsRegistrationOpen(auction, now))
        {
            return "This auction is not open for registration.";
        }

        return null;
    }

    private static AuctionRegistrationResult Fail(string message, int statusCode = 400) =>
        AuctionRegistrationResult.Fail(message, statusCode);
}
