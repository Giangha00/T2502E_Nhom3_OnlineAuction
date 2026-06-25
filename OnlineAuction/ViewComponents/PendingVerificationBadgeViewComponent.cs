using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;

namespace OnlineAuction.ViewComponents;

public class PendingVerificationBadgeViewComponent : ViewComponent
{
    private readonly IAdminAuctionVerificationService _verificationService;

    public PendingVerificationBadgeViewComponent(IAdminAuctionVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await _verificationService.GetPendingCountAsync();
        return View(count);
    }
}
