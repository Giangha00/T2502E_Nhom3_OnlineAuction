using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Models;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;
using System.Security.Claims;

namespace OnlineAuction.Controllers;

public class RefundController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;

    public RefundController(
        IPaymentService paymentService,
        INotificationService notificationService)
    {
        _paymentService = paymentService;
        _notificationService = notificationService;
    }

    public IActionResult Index()
    {
        var model = new RefundPageViewModel
        {
            RecentOrders = _paymentService.GetRefundEligibleOrders(),
            RefundReasons =
            [
                new RefundReasonOption { Id = "not-as-described", Label = "Item not as described in listing" },
                new RefundReasonOption { Id = "damaged", Label = "Item arrived damaged" },
                new RefundReasonOption { Id = "not-received", Label = "Item not received within delivery window" },
                new RefundReasonOption { Id = "counterfeit", Label = "Suspected counterfeit or misrepresented item" },
                new RefundReasonOption { Id = "duplicate-payment", Label = "Duplicate or incorrect payment" },
                new RefundReasonOption { Id = "other", Label = "Other (please describe)" }
            ],
            PolicyItems =
            [
                new RefundPolicyItem
                {
                    Title = "Eligible Cases",
                    Description = "Refunds may be approved when the item is materially different from the listing, arrives damaged, is not delivered, or payment was processed in error."
                },
                new RefundPolicyItem
                {
                    Title = "Non-Eligible Cases",
                    Description = "Winning bids cannot be refunded due to buyer's remorse. Bids placed intentionally are binding per auction rules."
                },
                new RefundPolicyItem
                {
                    Title = "Review Timeline",
                    Description = "Refund requests are reviewed within 3–5 business days. You will be notified by email once a decision is made."
                },
                new RefundPolicyItem
                {
                    Title = "Refund Method",
                    Description = "Approved refunds are returned to your original payment method within 7–14 business days after approval."
                }
            ]
        };

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Confirmation(string? requestId, string? orderRef, string? reason, CancellationToken cancellationToken)
    {
        var model = new RefundConfirmationViewModel
        {
            RequestId = requestId ?? $"RF-{DateTime.UtcNow:yyyyMMdd}-0000",
            OrderReference = orderRef ?? "N/A",
            Reason = reason ?? "Not specified"
        };

        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var referenceId = Math.Abs((requestId ?? model.RequestId).GetHashCode());
            await _notificationService.CreateAndPushAsync(
                userId.Value,
                "Refund approved",
                $"Your refund request for order {model.OrderReference} has been approved.",
                NotificationType.Refund,
                $"/Refund/Confirmation?requestId={Uri.EscapeDataString(model.RequestId)}&orderRef={Uri.EscapeDataString(model.OrderReference)}",
                NotificationReferenceTypes.RefundApproved,
                referenceId,
                cancellationToken: cancellationToken);
        }

        return View(model);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
