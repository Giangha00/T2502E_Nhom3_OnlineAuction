using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineAuction;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserController(
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userService = userService;
        _userManager = userManager;
        _notificationService = notificationService;
        _localizer = localizer;
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
            var message = string.Join(
                " ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
            await PushProfileNotificationAsync(currentUserId, false, message);
            return RedirectToAction(nameof(Detail), new { id = currentUserId });
        }

        var result = await _userService.UpdateOwnProfileAsync(currentUserId, model);
        await PushProfileNotificationAsync(currentUserId, result.Success, result.Message);
        return RedirectToAction(nameof(Detail), new { id = currentUserId });
    }

    private Task PushProfileNotificationAsync(int userId, bool isSuccess, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Task.CompletedTask;
        }

        return _notificationService.CreateAndPushAsync(
            userId,
            isSuccess ? _localizer["Common_Success"] : _localizer["Common_Error"],
            message,
            NotificationType.System,
            $"/User/Detail/{userId}",
            referenceType: isSuccess
                ? NotificationReferenceTypes.ProfileUpdated
                : NotificationReferenceTypes.ProfileUpdateFailed,
            referenceId: userId);
    }
}
