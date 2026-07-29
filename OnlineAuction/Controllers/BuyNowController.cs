using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineAuction;
using OnlineAuction.Configurations;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class BuyNowController : Controller
{
    private const string UnavailableBuyNowMessage = "This item is no longer available.";

    private readonly IAuctionService _auctionService;
    private readonly IOrderCreationService _orderCreationService;
    private readonly IOrderService _orderService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BuyNowController(
        IAuctionService auctionService,
        IOrderCreationService orderCreationService,
        IOrderService orderService,
        IStringLocalizer<SharedResource> localizer)
    {
        _auctionService = auctionService;
        _orderCreationService = orderCreationService;
        _orderService = orderService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _auctionService.GetBuyNowIndexAsync();
        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = (await HttpContext.AuthenticateAsync(AuthSchemes.Admin)).Succeeded;
        var product = await _auctionService.GetProductDetailAsync(id, currentUserId, isAdmin);
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
            return Conflict(new { success = false, message = UnavailableBuyNowMessage });
        }

        var result = await _orderCreationService.CreatePendingPaymentOrderForBuyNowAsync(
            auctionId,
            userId.Value,
            cancellationToken);

        if (!result.Success)
        {
            return Conflict(new { success = false, message = result.Message ?? UnavailableBuyNowMessage });
        }

        var orderCount = await _orderService.CountPendingPaymentOrdersAsync(userId.Value);
        var message = result.Message switch
        {
            "Added to your orders." => _localizer["Js_BuyNow_AddedToCart"].Value,
            "Item is already in your orders." => _localizer["Js_BuyNow_AlreadyInCart"].Value,
            _ => string.IsNullOrWhiteSpace(result.Message)
                ? _localizer["Js_BuyNow_AddedToCart"].Value
                : result.Message
        };

        return Json(new
        {
            success = true,
            message,
            orderCount
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Please sign in to continue." });
        }

        try
        {
            var result = await _orderService.ClearAllBuyNowOrdersAsync(userId.Value, cancellationToken);
            var orderCount = await _orderService.CountPendingPaymentOrdersAsync(userId.Value);

            return Json(new
            {
                success = true,
                message = result.Message,
                clearedCount = result.ClearedCount,
                orderCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    private static bool CanViewBuyNowDetail(ProductDetailViewModel product) =>
        product.IsSeller || product.CanPurchaseBuyNow;

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
