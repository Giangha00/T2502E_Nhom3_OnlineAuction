using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.ViewModels.RolePermissions;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class RolePermissionController : BaseAdminController
{
    private readonly IPermissionService _permissionService;

    public RolePermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var roles = await _permissionService.GetStaffRolesAsync(cancellationToken);
        var items = new List<RolePermissionRoleItemViewModel>();

        foreach (var role in roles)
        {
            var permissionCount = role.Name == StaffRoleNames.Admin
                ? PermissionCodes.All.Length
                : (await _permissionService.GetPermissionIdsForRoleAsync(role.Id, cancellationToken)).Count;

            items.Add(new RolePermissionRoleItemViewModel
            {
                Id = role.Id,
                Name = role.Name,
                IsSuperRole = role.Name == StaffRoleNames.Admin,
                PermissionCount = permissionCount
            });
        }

        return View(new RolePermissionIndexViewModel { Roles = items });
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var roles = await _permissionService.GetStaffRolesAsync(cancellationToken);
        var role = roles.FirstOrDefault(item => item.Id == id);
        if (role is null)
        {
            return NotFound();
        }

        if (role.Name == StaffRoleNames.Admin)
        {
            TempData["ErrorMessage"] = "Admin role has all permissions automatically.";
            return RedirectToAction(nameof(Index));
        }

        var permissions = await _permissionService.GetAllPermissionsAsync(cancellationToken);
        var selectedIds = (await _permissionService.GetPermissionIdsForRoleAsync(id, cancellationToken)).ToHashSet();

        var model = new RolePermissionEditViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            IsSuperRole = false,
            Modules = permissions
                .GroupBy(permission => permission.Module)
                .OrderBy(group => group.Key)
                .Select(group => new RolePermissionModuleViewModel
                {
                    Module = group.Key,
                    Permissions = group
                        .Select(permission => new RolePermissionItemViewModel
                        {
                            Id = permission.Id,
                            Code = permission.Code,
                            Name = permission.Name,
                            Description = permission.Description,
                            IsSelected = selectedIds.Contains(permission.Id)
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Edit(RolePermissionSaveViewModel model, CancellationToken cancellationToken)
    {
        var result = await _permissionService.AssignPermissionsToRoleAsync(
            model.RoleId,
            model.PermissionIds,
            cancellationToken);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
