using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class BuyNowController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IOrderCreationService _orderCreationService;

    public BuyNowController(
        IAuctionService auctionService,
        IOrderCreationService orderCreationService)
    {
        _auctionService = auctionService;
        _orderCreationService = orderCreationService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _auctionService.GetBuyNowIndexAsync();
        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _auctionService.GetProductDetailAsync(id);
        if (product is null || !product.HasBuyNow)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int auctionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Please sign in to continue." });
        }

        var listing = await _auctionService.GetAuctionByIdAsync(auctionId);
        if (listing is null || !listing.HasBuyNow)
        {
            return NotFound(new { success = false, message = "Product not found." });
        }

        var result = await _orderCreationService.CreatePendingPaymentOrderForBuyNowAsync(
            auctionId,
            userId.Value,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            redirectUrl = Url.Action("Index", "Order")
        });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
