using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class AboutUsController : Controller
{
    public IActionResult Index()
    {
        return View("About");
    }
}
