using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Complaints;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class ComplaintController : BaseAdminController
{
    private readonly IAdminComplaintService _complaintService;
    private readonly ICurrentUserContext _currentUserContext;

    public ComplaintController(
        IAdminComplaintService complaintService,
        ICurrentUserContext currentUserContext)
    {
        _complaintService = complaintService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ComplaintsReview)]
    public async Task<IActionResult> Index(ComplaintFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = await _complaintService.GetComplaintsAsync(filter, cancellationToken);
        return View(model);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ComplaintsReview)]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await _complaintService.GetComplaintDetailAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["ErrorMessage"] = "Complaint not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.ComplaintsReview)]
    public async Task<IActionResult> UpdateStatus(ComplaintUpdateStatusViewModel model, CancellationToken cancellationToken)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync(cancellationToken);
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _complaintService.UpdateStatusAsync(
            model.ComplaintId,
            model.Action,
            adminId.Value,
            model.AdminNotes,
            model.ResolutionNote,
            cancellationToken);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return result.Success && model.Action == ComplaintStatusActions.AddNote
            ? RedirectToAction(nameof(Details), new { id = model.ComplaintId })
            : result.Success && model.Action is ComplaintStatusActions.Approve or ComplaintStatusActions.Reject or ComplaintStatusActions.Close
                ? RedirectToAction(nameof(Index), new { Status = ComplaintStatuses.Pending })
                : RedirectToAction(nameof(Details), new { id = model.ComplaintId });
    }
}
