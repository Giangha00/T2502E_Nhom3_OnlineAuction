using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
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
    [RequirePermission(PermissionCodes.AuctionsVerify)]
    public async Task<IActionResult> Index(AuctionVerificationFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = await _verificationService.GetPendingVerificationsAsync(filter, cancellationToken);
        return ListOrDefaultView(model, "_AuctionVerificationList");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsVerify)]
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
    [RequirePermission(PermissionCodes.AuctionsVerify)]
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
    [RequirePermission(PermissionCodes.AuctionsVerify)]
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
