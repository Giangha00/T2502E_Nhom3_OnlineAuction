using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class UserController : BaseAdminController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<IActionResult> Index(UserFilterViewModel filter)
    {
        var model = await _userService.GetUsersAsync(filter);
        return ListOrDefaultView(model, "_UserList");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Create()
    {
        return View(await _userService.BuildCreateFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await _userService.BuildCreateFormAsync());
        }

        var result = await _userService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _userService.BuildCreateFormAsync());
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _userService.GetEditFormAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Edit(UserFormViewModel model, bool returnToDetails = false)
    {
        if (!ModelState.IsValid)
        {
            if (returnToDetails && model.Id.HasValue)
            {
                return await DetailsPageResultAsync(model.Id.Value, model);
            }

            var editModel = await _userService.GetEditFormAsync(model.Id ?? 0);
            return editModel is null ? NotFound() : View(editModel);
        }

        var result = await _userService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            if (returnToDetails && model.Id.HasValue)
            {
                return await DetailsPageResultAsync(model.Id.Value, model);
            }

            var editModel = await _userService.GetEditFormAsync(model.Id ?? 0);
            return editModel is null ? NotFound() : View(editModel);
        }

        TempData["SuccessMessage"] = result.Message;

        if (returnToDetails && model.Id.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = model.Id.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<IActionResult> Details(int id)
    {
        return await DetailsPageResultAsync(id);
    }

    private async Task<IActionResult> DetailsPageResultAsync(int id, UserFormViewModel? postedForm = null)
    {
        var profile = await _userService.GetDetailsAsync(id);
        if (profile is null)
        {
            return NotFound();
        }

        var canEdit = AdminPermissionHelper.Can(User, PermissionCodes.UsersManage);
        UserFormViewModel? editForm = null;

        if (canEdit)
        {
            editForm = postedForm ?? await _userService.GetEditFormAsync(id);
            if (editForm is null)
            {
                return NotFound();
            }

            if (postedForm is not null)
            {
                // Keep dropdown / permission options populated after validation errors.
                var fresh = await _userService.GetEditFormAsync(id);
                if (fresh is not null)
                {
                    editForm.RoleOptions = fresh.RoleOptions;
                    editForm.StatusOptions = fresh.StatusOptions;
                    editForm.AvailablePermissions = fresh.AvailablePermissions;
                    editForm.CurrentAvatarUrl ??= fresh.CurrentAvatarUrl;
                }
            }
        }

        return View("Details", new UserDetailsPageViewModel
        {
            Profile = profile,
            EditForm = editForm,
            CanEdit = canEdit && editForm is not null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> BulkAction(UserBulkActionViewModel model)
    {
        var result = await _userService.ExecuteBulkActionAsync(model);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}