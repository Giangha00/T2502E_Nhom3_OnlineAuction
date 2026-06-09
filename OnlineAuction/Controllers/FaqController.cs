using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class FaqController : Controller
{
  
    public IActionResult Index()
    {
        return View();
    }
}