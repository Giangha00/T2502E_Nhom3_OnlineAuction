using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Models;
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
        var currentUserId = GetCurrentUserId();
        var product = await _auctionService.GetProductDetailAsync(id, currentUserId);
        if (product is null || !product.HasBuyNow || !CanViewBuyNowDetail(product))
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int auctionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Please sign in to continue." });
        }

        var product = await _auctionService.GetProductDetailAsync(auctionId, userId);
        if (product is null || !product.HasBuyNow || !product.CanPurchaseBuyNow)
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
            redirectUrl = Url.Action("Index", "Order", new { added = 1 })
        });
    }

    private static bool CanViewBuyNowDetail(ProductDetailViewModel product) =>
        product.IsSeller || product.CanPurchaseBuyNow;

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
