using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderCreationService _orderCreationService;

    public OrderController(
        IOrderService orderService,
        IOrderCreationService orderCreationService)
    {
        _orderService = orderService;
        _orderCreationService = orderCreationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        await _orderCreationService.FinalizeExpiredAuctionsAsync();
        var model = await _orderService.BuildOrderPageAsync(userId.Value);
        if (model is null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(string paymentMethod)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        var result = await _orderService.CompleteOrderAsync(userId.Value, paymentMethod);
        if (!result.Success)
        {
            TempData["OrderError"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["OrderMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    protected int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
