using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;
using System.Security.Claims;

namespace OnlineAuction.Controllers;

public class RefundController : Controller
{
    private readonly IRefundComplaintService _refundComplaintService;

    public RefundController(IRefundComplaintService refundComplaintService)
    {
        _refundComplaintService = refundComplaintService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var recentOrders = userId.HasValue
            ? await _refundComplaintService.GetEligibleOrdersAsync(userId.Value, cancellationToken)
            : [];

        var model = new RefundPageViewModel
        {
            RecentOrders = recentOrders.ToList(),
            IsAuthenticated = userId.HasValue,
            RefundReasons =
            [
                new RefundReasonOption { Id = ComplaintReasonCodes.NotAsDescribed, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.NotAsDescribed] },
                new RefundReasonOption { Id = ComplaintReasonCodes.Damaged, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.Damaged] },
                new RefundReasonOption { Id = ComplaintReasonCodes.NotReceived, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.NotReceived] },
                new RefundReasonOption { Id = ComplaintReasonCodes.Counterfeit, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.Counterfeit] },
                new RefundReasonOption { Id = ComplaintReasonCodes.DuplicatePayment, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.DuplicatePayment] },
                new RefundReasonOption { Id = ComplaintReasonCodes.Other, Label = ComplaintReasonCodes.Labels[ComplaintReasonCodes.Other] }
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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(RefundSubmitViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var result = await _refundComplaintService.SubmitAsync(userId.Value, model, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            requestId = result.RequestReference,
            redirectUrl = Url.Action(nameof(Confirmation), new { requestId = result.RequestReference })
        });
    }

    [Authorize]
    public async Task<IActionResult> Confirmation(string? requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Forbid();
        }

        var model = await _refundComplaintService.GetConfirmationAsync(userId.Value, requestId, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
