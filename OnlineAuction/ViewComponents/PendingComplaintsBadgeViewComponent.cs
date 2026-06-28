using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;

namespace OnlineAuction.ViewComponents;

public class PendingComplaintsBadgeViewComponent : ViewComponent
{
    private readonly IAdminComplaintService _complaintService;

    public PendingComplaintsBadgeViewComponent(IAdminComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await _complaintService.GetPendingCountAsync();
        return View(count);
    }
}
