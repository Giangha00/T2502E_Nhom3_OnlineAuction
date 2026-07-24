using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var currentUserIdText = _userManager.GetUserId(User);
        int? viewerUserId = int.TryParse(currentUserIdText, out var currentUserId) ? currentUserId : null;

        var model = await _userService.GetPublicProfileAsync(id, viewerUserId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    public async Task<IActionResult> UpdateProfile(UserProfileEditViewModel model)
    {
        var currentUserIdText = _userManager.GetUserId(User);
        if (!int.TryParse(currentUserIdText, out var currentUserId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
            return RedirectToAction(nameof(Detail), new { id = currentUserId });
        }

        var result = await _userService.UpdateOwnProfileAsync(currentUserId, model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = currentUserId });
    }
}
