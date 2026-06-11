using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class AboutUsController : Controller
{
    public IActionResult About()
    {
        return View("About");
    }
}
