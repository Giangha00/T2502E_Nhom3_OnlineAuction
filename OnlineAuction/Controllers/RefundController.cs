using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class RefundController : Controller
{
    private readonly IPaymentService _paymentService;

    public RefundController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
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

    public IActionResult Confirmation(string? requestId, string? orderRef, string? reason)
    {
        var model = new RefundConfirmationViewModel
        {
            RequestId = requestId ?? $"RF-{DateTime.UtcNow:yyyyMMdd}-0000",
            OrderReference = orderRef ?? "N/A",
            Reason = reason ?? "Not specified"
        };

        return View(model);
    }
}
