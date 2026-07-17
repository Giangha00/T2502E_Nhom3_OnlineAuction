using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services;

namespace OnlineAuction.Controllers;

/// <summary>
/// Dev-only endpoints used by <c>scripts/smoke/Invoke-ReleaseSmoke.ps1</c>.
/// Blocked unless Development + SmokeTesting:Enabled.
/// </summary>
[Route("Smoke")]
[ApiController]
public sealed class SmokeController : ControllerBase
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SmokeTestingSettings _settings;
    private readonly PlatformFeeSettings _feeSettings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmokeController> _logger;

    public SmokeController(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<SmokeTestingSettings> settings,
        IOptions<PlatformFeeSettings> feeSettings,
        IHostEnvironment environment,
        ILogger<SmokeController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _settings = settings.Value;
        _feeSettings = feeSettings.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("ConfirmEmail")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ConfirmEmail([FromForm] string email)
    {
        if (!IsSmokeAllowed())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, message = "email is required.", caseId = "AUTH-REG-01" });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return NotFound(new { success = false, message = "User not found.", caseId = "AUTH-REG-01" });
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.Join("; ", update.Errors.Select(e => e.Description)),
                    caseId = "AUTH-REG-01"
                });
            }
        }

        _logger.LogWarning("SmokeTesting confirmed email for {Email}.", email);
        return Ok(new { success = true, message = "Email confirmed (smoke).", caseId = "AUTH-REG-01", userId = user.Id });
    }

    [HttpGet("PickAuction")]
    [AllowAnonymous]
    public async Task<IActionResult> PickAuction(CancellationToken cancellationToken)
    {
        if (!IsSmokeAllowed())
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .Where(a => a.RequiresRegistration)
            .Where(a => a.Status == AuctionStatuses.Live
                || a.Status == AuctionStatuses.EndingSoon
                || a.Status == AuctionStatuses.Scheduled)
            .Where(a => a.RegistrationStartDate <= now && a.RegistrationEndDate > now)
            .Where(a => a.EndDate > now)
            .OrderByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Status,
                a.CurrentPrice,
                a.BidStep,
                a.StartingPrice,
                a.RequiresRegistration,
                productName = a.Product.Name,
                sellerId = a.Product.SellerId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (auction is null)
        {
            return NotFound(new
            {
                success = false,
                message = "No live/registerable auction found for smoke. Reseed or pass -AuctionId."
            });
        }

        return Ok(new { success = true, auction });
    }

    [HttpPost("CompleteRegistrationDeposit")]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CompleteRegistrationDeposit(
        [FromForm] int auctionId,
        CancellationToken cancellationToken)
    {
        if (!IsSmokeAllowed())
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Sign in required.", caseId = "AUCTION_REG-03" });
        }

        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            return NotFound(new { success = false, message = "Auction not found.", caseId = "AUCTION_REG-03" });
        }

        if (auction.Product.SellerId == userId)
        {
            return BadRequest(new
            {
                success = false,
                message = "Seller cannot register on own auction.",
                caseId = "AUCTION_REG-03"
            });
        }

        var now = DateTime.UtcNow;
        var registration = await _dbContext.AuctionRegistrations
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.UserId == userId, cancellationToken);

        if (registration is null)
        {
            registration = new AuctionRegistration
            {
                AuctionId = auctionId,
                UserId = userId,
                Status = AuctionRegistrationStatuses.Pending,
                RegisteredAt = now,
                CreatedAt = now
            };
            _dbContext.AuctionRegistrations.Add(registration);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (registration.Status == AuctionRegistrationStatuses.Approved
            && registration.Deposits.Any(d => d.Status == AuctionRegistrationDepositStatuses.Paid))
        {
            return Ok(new
            {
                success = true,
                message = "Already registered with paid deposit.",
                caseId = "AUCTION_REG-03",
                auctionId,
                registrationId = registration.Id,
                mode = "AlreadyApproved"
            });
        }

        var deposit = registration.Deposits
            .OrderByDescending(d => d.Id)
            .FirstOrDefault(d => d.Status == AuctionRegistrationDepositStatuses.Pending);

        if (deposit is null)
        {
            decimal amount;
            try
            {
                var productValue = auction.Product.EstimatedValue ?? auction.StartingPrice;
                amount = MarketplaceFeeCalculator.CalculateRegistrationDeposit(productValue, _feeSettings);
            }
            catch (InvalidOperationException)
            {
                amount = Math.Max(1m, _feeSettings.MinimumRegistrationDeposit);
            }

            deposit = new AuctionRegistrationDeposit
            {
                AuctionId = auctionId,
                UserId = userId,
                AuctionRegistrationId = registration.Id,
                Amount = amount,
                Status = AuctionRegistrationDepositStatuses.Pending,
                PayPalOrderId = $"SMOKE-{Guid.NewGuid():N}",
                CreatedAt = now
            };
            _dbContext.AuctionRegistrationDeposits.Add(deposit);
        }

        deposit.Status = AuctionRegistrationDepositStatuses.Paid;
        deposit.PayPalCaptureId ??= $"SMOKE-CAPTURE-{Guid.NewGuid():N}";
        deposit.PaidAt = now;
        deposit.UpdatedAt = now;

        registration.Status = AuctionRegistrationStatuses.Approved;
        registration.ReviewedAt = now;
        registration.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "SmokeTesting approved registration+deposit for user {UserId} on auction {AuctionId}.",
            userId,
            auctionId);

        return Ok(new
        {
            success = true,
            message = "Registration deposit completed (smoke bypass).",
            caseId = "AUCTION_REG-03",
            auctionId,
            registrationId = registration.Id,
            depositId = deposit.Id,
            depositAmount = deposit.Amount,
            mode = "SmokeBypass"
        });
    }

    private bool IsSmokeAllowed() =>
        _environment.IsDevelopment() && _settings.Enabled;
}
