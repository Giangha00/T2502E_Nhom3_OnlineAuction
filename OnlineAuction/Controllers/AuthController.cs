using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Controllers;

public class AuthController : Controller
{
    private const string SessionLoggedInKey = "IsLoggedIn";

    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["AuthError"] = "Please enter email and password.";
            return View();
        }

        HttpContext.Session.SetString(SessionLoggedInKey, "true");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult SignUp() => View();

    [HttpPost]
    public IActionResult SignUp(string fullName, string email, string phoneNumber, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["AuthError"] = "Please fill in all required fields.";
            return View();
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            TempData["AuthError"] = "Passwords do not match.";
            return View();
        }

        HttpContext.Session.SetString(SessionLoggedInKey, "true");
        HttpContext.Session.SetString("UserName", fullName.Trim());
        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
