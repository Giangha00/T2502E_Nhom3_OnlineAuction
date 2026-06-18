using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    public IActionResult Index()
    {
        return View();
    }
}
