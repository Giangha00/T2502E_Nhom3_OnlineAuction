using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderCreationService _orderCreationService;
    private readonly IOrderPaymentService _orderPaymentService;

    public OrderController(
        IOrderService orderService,
        IOrderCreationService orderCreationService,
        IOrderPaymentService orderPaymentService)
    {
        _orderService = orderService;
        _orderCreationService = orderCreationService;
        _orderPaymentService = orderPaymentService;
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
    public async Task<IActionResult> Complete(CompleteOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            var error = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Please complete all required fields.";

            TempData["OrderError"] = error;
            return RedirectToAction(nameof(Index));
        }

        var result = await _orderService.CompleteOrderAsync(userId.Value, request);
        if (!result.Success)
        {
            TempData["OrderError"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(request.PaymentMethod, "paypal", StringComparison.OrdinalIgnoreCase))
        {
            var returnUrl = Url.Action("PayPalReturn", "Payment", null, Request.Scheme)!;
            var cancelUrl = Url.Action("PayPalCancel", "Payment", null, Request.Scheme)!;
            var paypalResult = await _orderPaymentService.InitiatePayPalCheckoutAsync(
                userId.Value,
                request.SelectedOrderIds,
                returnUrl,
                cancelUrl);

            if (!paypalResult.Success || string.IsNullOrWhiteSpace(paypalResult.ApprovalUrl))
            {
                TempData["OrderError"] = paypalResult.ErrorMessage ?? "Unable to start PayPal checkout.";
                return RedirectToAction(nameof(Index));
            }

            return Redirect(paypalResult.ApprovalUrl);
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
