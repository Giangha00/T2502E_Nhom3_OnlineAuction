using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class AuctionVerificationController : BaseAdminController
{
    private readonly IAdminAuctionVerificationService _verificationService;
    private readonly ICurrentUserContext _currentUserContext;

    public AuctionVerificationController(
        IAdminAuctionVerificationService verificationService,
        ICurrentUserContext currentUserContext)
    {
        _verificationService = verificationService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuctionVerificationFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = await _verificationService.GetPendingVerificationsAsync(filter, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await _verificationService.GetVerificationDetailAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["ErrorMessage"] = "Auction not found or no longer pending verification.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync(cancellationToken);
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _verificationService.ApproveAsync(id, adminId.Value, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return result.Success
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string rejectReason, CancellationToken cancellationToken)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync(cancellationToken);
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _verificationService.RejectAsync(id, adminId.Value, rejectReason, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return result.Success
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id });
    }
}
