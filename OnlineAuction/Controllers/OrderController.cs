using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderCreationService _orderCreationService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly AuctionHouseDbContext _dbContext;

    public OrderController(
        IOrderService orderService,
        IOrderCreationService orderCreationService,
        IOrderPaymentService orderPaymentService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        AuctionHouseDbContext dbContext)
    {
        _orderService = orderService;
        _orderCreationService = orderCreationService;
        _orderPaymentService = orderPaymentService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _dbContext = dbContext;
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
    public async Task<IActionResult> ClearBuyNow()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        var now = DateTime.UtcNow;
        var buyNowOrders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BuyerId == userId.Value &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.OrderSource == OrderSources.BuyNow)
            .ToListAsync();

        foreach (var order in buyNowOrders)
        {
            order.Status = OrderStatuses.Cancelled;
            order.UpdatedAt = now;
            await OrderCancellationHelper.ApplyCancellationSideEffectsAsync(_dbContext, order, now);
        }

        if (buyNowOrders.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
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
                ?? _notifyLocalizer[NotificationKeys.PaymentCompleteFieldsMessage];

            await NotifyPaymentIssueAsync(userId.Value, error);
            return RedirectToAction(nameof(Index));
        }

        var result = await _orderService.CompleteOrderAsync(userId.Value, request);
        if (!result.Success)
        {
            await NotifyPaymentIssueAsync(userId.Value, result.Message);
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
                await NotifyPaymentIssueAsync(
                    userId.Value,
                    paypalResult.ErrorMessage ?? _notifyLocalizer[NotificationKeys.PaymentUnableStartCheckoutMessage]);
                return RedirectToAction(nameof(Index));
            }

            return Redirect(paypalResult.ApprovalUrl);
        }

        // COD success is already pushed inside OrderService.CompleteOrderAsync.
        return RedirectToAction(nameof(Index));
    }

    private async Task NotifyPaymentIssueAsync(int userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await _notificationService.CreateAndPushAsync(
            userId,
            _notifyLocalizer[NotificationKeys.PaymentFailedTitle],
            message,
            NotificationType.Payment,
            "/Order",
            NotificationReferenceTypes.PaymentFailed,
            userId,
            debounceWindow: TimeSpan.FromMinutes(2));
    }

    protected int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
