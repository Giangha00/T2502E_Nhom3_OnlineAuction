using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class AuctionController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IBidService _bidService;

    public AuctionController(IAuctionService auctionService, IBidService bidService)
    {
        _auctionService = auctionService;
        _bidService = bidService;
    }

    public IActionResult Index()
    {
        var model = _auctionService.GetAuctionIndex();
        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _auctionService.GetProductDetailAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(int auctionId, decimal amount)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { success = false, message = "Please sign in to place a bid." });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            endDate = result.EndDate?.ToUniversalTime().ToString("o"),
            bidHistory = result.BidHistory?.Select(bid => new
            {
                bidderName = bid.BidderName,
                amount = bid.Amount,
                bidTime = bid.BidTime,
                isWinning = bid.IsWinning
            })
        });
    }
}
