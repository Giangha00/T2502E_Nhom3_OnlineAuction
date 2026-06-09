using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class PolicyController : Controller
{
    public IActionResult Index()
    {
        return View("Policy");
    }
}
