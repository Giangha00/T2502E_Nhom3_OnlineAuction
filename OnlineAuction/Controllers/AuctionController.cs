using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class AuctionController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IBidService _bidService;
    private readonly IAuctionRegistrationService _registrationService;

    public AuctionController(
        IAuctionService auctionService,
        IBidService bidService,
        IAuctionRegistrationService registrationService)
    {
        _auctionService = auctionService;
        _bidService = bidService;
        _registrationService = registrationService;
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
            registrationCount = result.RegistrationCount
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
