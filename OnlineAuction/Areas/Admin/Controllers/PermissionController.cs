using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class PermissionController : BaseAdminController
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [RequirePermission(PermissionCodes.PermissionsView)]
    public async Task<IActionResult> Index(int? userId, CancellationToken cancellationToken)
    {
        var canManage = AdminPermissionHelper.Can(User, PermissionCodes.PermissionsManage);
        var model = await _permissionService.GetPermissionManagementViewModelAsync(
            canManage,
            userId,
            cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.PermissionsManage)]
    public async Task<IActionResult> SaveUser(int userId, List<int> permissionIds, CancellationToken cancellationToken)
    {
        var result = await _permissionService.SaveUserPermissionsAsync(
            userId,
            permissionIds ?? [],
            cancellationToken);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { userId });
    }
}
