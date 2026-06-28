using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
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
    public IActionResult Create()
    {
        var model = _userService.BuildCreateForm();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(_userService.BuildCreateForm());
        }

        var result = await _userService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(_userService.BuildCreateForm());
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
    public async Task<IActionResult> Edit(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var editModel = await _userService.GetEditFormAsync(model.Id ?? 0);
            return editModel is null ? NotFound() : View(editModel);
        }

        var result = await _userService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var editModel = await _userService.GetEditFormAsync(model.Id ?? 0);
            return editModel is null ? NotFound() : View(editModel);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _userService.GetDetailsAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
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