using OnlineAuction.Models;
using OnlineAuction.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]/[action]")]


public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email
        };

        var result =
            await _userManager.CreateAsync(
                user,
                model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(
                user,
                false);

            return RedirectToAction(
                "Index",
                "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                "",
                error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (user == null)
        {
            ModelState.AddModelError(
                "",
                "User not found");

            return View(model);
        }

        var result =
            await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                false);

        if (result.Succeeded)
        {
            return RedirectToAction(
                "Index",
                "Home");
        }

        ModelState.AddModelError(
            "",
            "Login failed");

        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(
            "Index",
            "Home");
    }
}