using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class AuctionController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IBidService _bidService;
    private readonly IAuctionRegistrationService _registrationService;
    private readonly IRegistrationDepositService _registrationDepositService;
    private readonly IRegistrationDepositRefundService _depositRefundService;
    private readonly IBidRateLimitService _bidRateLimitService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<AuctionController> _logger;

    public AuctionController(
        IAuctionService auctionService,
        IBidService bidService,
        IAuctionRegistrationService registrationService,
        IRegistrationDepositService registrationDepositService ,
        IRegistrationDepositRefundService depositRefundService,
        IBidRateLimitService bidRateLimitService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AuctionController> logger)
    {
        _auctionService = auctionService;
        _bidService = bidService;
        _registrationService = registrationService;
        _registrationDepositService = registrationDepositService;
        _depositRefundService = depositRefundService;
        _bidRateLimitService = bidRateLimitService;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _auctionService.GetAuctionIndexAsync();
        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _auctionService.GetProductDetailAsync(id, GetCurrentUserId());
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> BidState(int id, CancellationToken cancellationToken)
    {
        var state = await _bidService.GetBidStateAsync(id, cancellationToken);
        if (state is null)
        {
            return NotFound();
        }

        return Json(new
        {
            auctionId = state.AuctionId,
            currentPrice = state.CurrentPrice,
            bidCount = state.BidCount,
            minNextBid = state.MinNextBid,
            endDate = DateTimeUtilities.AsUtc(state.EndDate).ToString("o"),
            isEnded = state.IsEnded,
            bidHistory = state.BidHistory.Select(bid => new
            {
                bidderName = bid.BidderName,
                amount = bid.Amount,
                bidTime = bid.BidTime,
                isWinning = bid.IsWinning,
                status = bid.Status
            })
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int auctionId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Please sign in to register." });
        }

        // If this auction requires a deposit, public Register endpoint must not
        // approve registration directly. Instruct client to initiate deposit flow.
        var auctionItem = await _auctionService.GetAuctionByIdAsync(auctionId);
        if (auctionItem is null)
        {
            return NotFound(new { success = false, message = "Auction not found." });
        }

        if (auctionItem.RequiresRegistration)
        {
            var initiateUrl = Url.Action(nameof(InitiateDeposit), "Auction", new { auctionId }, Request.Scheme);
            return StatusCode(410, new
            {
                success = false,
                message = "This auction requires a deposit. Please complete the deposit flow to register.",
                initiateDepositUrl = initiateUrl
            });
        }

        var result = await _registrationService.RegisterAsync(auctionId, userId.Value);
        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { success = false, message = result.Message }),
                401 => Unauthorized(new { success = false, message = result.Message }),
                _ => BadRequest(new { success = false, message = result.Message })
            };
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            status = result.Status,
            registrationCount = result.RegistrationCount
        });
    }
    
    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefundDeposit(long depositId)
    {
        // Restricted to Admin/internal workers only in production.
        var result = await _depositRefundService.RefundDepositAsync(depositId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            auctionId = result.AuctionId,
            depositAmount = result.DepositAmount
        });
    }
    
    
    [HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> InitiateDeposit(int auctionId)
{
    // Lấy user hiện tại từ Claims
    var userId = GetCurrentUserId();

    if (!userId.HasValue)
    {
        return Unauthorized(new
        {
            success = false,
            message = "Bạn cần đăng nhập để đăng ký đấu giá."
        });
    }

    // PayPal thanh toán thành công sẽ redirect về URL này
    var returnUrl = Url.Action(
        nameof(DepositPayPalReturn),
        "Auction",
        null,
        Request.Scheme)!;

    // PayPal bị user hủy sẽ redirect về URL này
    var cancelUrl = Url.Action(
        nameof(DepositPayPalCancel),
        "Auction",
        null,
        Request.Scheme)!;

    // Gọi service xử lý toàn bộ logic:
    // validate auction
    // tính tiền cọc
    // tạo registration pending
    // tạo deposit pending
    // tạo PayPal order
    var result = await _registrationDepositService.InitiateDepositAsync(
        auctionId,
        userId.Value,
        returnUrl,
        cancelUrl);

    if (!result.Success)
    {
        return BadRequest(new
        {
            success = false,
            message = result.Message
        });
    }

    // Trả approvalUrl cho frontend
    // Frontend sẽ window.location.href = approvalUrl
    return Json(new
    {
        success = true,
        message = result.Message,
        approvalUrl = result.ApprovalUrl,
        depositAmount = result.DepositAmount
    });
}

[HttpGet]
[Authorize]
public async Task<IActionResult> DepositPayPalReturn(string token)
{
    // token PayPal gửi về chính là PayPalOrderId
    var userId = GetCurrentUserId();

    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    // Capture tiền từ PayPal
    // Nếu capture thành công:
    // deposit.status = paid
    // registration.status = approved
    var result = await _registrationDepositService.CaptureDepositAsync(
        userId.Value,
        token);

    if (!result.Success)
    {
        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    TempData["SuccessMessage"] = result.Message;

    // Quay lại trang chi tiết phiên đấu giá
    return RedirectToAction(nameof(Detail), new
    {
        id = result.AuctionId
    });
}

[HttpGet]
[Authorize]
public async Task<IActionResult> DepositPayPalCancel(string token)
{
    var userId = GetCurrentUserId();

    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    // User hủy thanh toán trên PayPal
    // deposit.status = cancelled
    // registration.status = cancelled
    var result = await _registrationDepositService.CancelDepositAsync(
        userId.Value,
        token);

    TempData["ErrorMessage"] = result.Message;

    return RedirectToAction(nameof(Detail), new
    {
        id = result.AuctionId
    });
}
    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int auctionId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Please sign in." });
        }

        var result = await _registrationService.CancelRegistrationAsync(auctionId, userId.Value);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            status = result.Status,
            registrationCount = result.RegistrationCount,
            refundedAmount = result.RefundedAmount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(int auctionId, decimal amount)
    {
        var userAuth = await HttpContext.AuthenticateAsync(AuthSchemes.User);
        if (!userAuth.Succeeded)
        {
            return Unauthorized(new { success = false, message = "Please sign in to place a bid." });
        }

        var userIdClaim = userAuth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var bidderId))
        {
            return Unauthorized(new { success = false, message = "Please sign in to place a bid." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var rateLimit = await _bidRateLimitService.CheckAsync(auctionId, bidderId, ipAddress);
        if (!rateLimit.IsAllowed)
        {
            _logger.LogWarning(
                "Bid rejected by rate limit for auction {AuctionId}, user {UserId}, IP {IpAddress}.",
                auctionId,
                bidderId,
                ipAddress);

            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { success = false, message = _localizer["Bid_RateLimit_Message"].Value });
        }

        var result = await _bidService.PlaceBidAsync(auctionId, bidderId, amount);
        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { success = false, message = result.Message }),
                401 => Unauthorized(new { success = false, message = result.Message }),
                _ => BadRequest(new { success = false, message = result.Message })
            };
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            currentPrice = result.CurrentPrice,
            bidCount = result.BidCount,
            minNextBid = result.MinNextBid,
            endDate = result.EndDate is null ? null : DateTimeUtilities.AsUtc(result.EndDate.Value).ToString("o"),
            bidHistory = result.BidHistory?.Select(bid => new
            {
                bidderName = bid.BidderName,
                amount = bid.Amount,
                bidTime = bid.BidTime,
                isWinning = bid.IsWinning,
                status = bid.Status
            })
        });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
