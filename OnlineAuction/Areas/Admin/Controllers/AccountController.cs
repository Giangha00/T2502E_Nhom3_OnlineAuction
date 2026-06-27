using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Data.Seeders;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionService _permissionService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService)
    {
        _userManager = userManager;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var adminAuth = await HttpContext.AuthenticateAsync(AuthSchemes.Admin);
        if (adminAuth.Succeeded)
        {
            var userIdValue = adminAuth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var userId))
            {
                var existingUser = await _userManager.FindByIdAsync(userId.ToString());
                if (existingUser is not null && await IdentityRoleSyncService.HasAdminAccessAsync(_userManager, existingUser))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!await _userManager.CheckPasswordAsync(user, model.Password))
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Account locked");
                return View(model);
            }

            await _userManager.AccessFailedAsync(user);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Account locked");
            return View(model);
        }

        if (!await IdentityRoleSyncService.HasAdminAccessAsync(_userManager, user))
        {
            ModelState.AddModelError(string.Empty, "Admin access required.");
            return View(model);
        }

        if (user.Status != UserStatus.Active)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated");
            return View(model);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await SignInAdminAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Admin);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAdminAsync(ApplicationUser user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FullName", user.FullName)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id);
        claims.AddRange(permissions.Select(permission =>
            new Claim(PermissionClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, AuthSchemes.Admin);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddHours(8) : null
        };

        await HttpContext.SignInAsync(AuthSchemes.Admin, principal, properties);
    }
}
