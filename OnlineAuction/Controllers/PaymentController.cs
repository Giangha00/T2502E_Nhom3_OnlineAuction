using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderPaymentService _orderPaymentService;

    public PaymentController(
        IPaymentService paymentService,
        IOrderPaymentService orderPaymentService)
    {
        _paymentService = paymentService;
        _orderPaymentService = orderPaymentService;
    }

    public IActionResult Index()
    {
        var model = _paymentService.GetPaymentInformation();
        return View(model);
    }

    public IActionResult Checkout(int? auctionId)
    {
        var model = _paymentService.BuildCheckout(auctionId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Confirmation(int orderId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = await _orderPaymentService.GetPaidOrderConfirmationAsync(userId.Value, orderId);
        if (model is null)
        {
            return RedirectToAction("Index", "Order");
        }

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> PayPalReturn(string? token)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["OrderError"] = "PayPal did not return a valid checkout token.";
            return RedirectToAction("Index", "Order");
        }

        var captureResult = await _orderPaymentService.CapturePayPalCheckoutAsync(userId.Value, token);
        if (!captureResult.Success)
        {
            TempData["OrderError"] = captureResult.ErrorMessage ?? "Payment could not be completed.";
            return RedirectToAction("Index", "Order");
        }

        TempData["PaymentSuccess"] = true;
        return RedirectToAction(nameof(Confirmation), new { orderId = captureResult.PrimaryOrderId });
    }

    [Authorize]
    public async Task<IActionResult> PayPalCancel(string? token)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await _orderPaymentService.CancelPayPalCheckoutAsync(userId.Value, token);
        }

        TempData["OrderError"] = "PayPal payment was cancelled. Your order is still pending payment.";
        return RedirectToAction("Index", "Order");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
