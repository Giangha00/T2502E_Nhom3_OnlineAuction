using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class AuthController : Controller
{
    // GET
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult SignUp()
    {
        return View();
    }
}