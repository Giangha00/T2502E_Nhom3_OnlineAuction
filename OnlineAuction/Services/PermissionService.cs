using Microsoft.AspNetCore.Identity;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PermissionService : IPermissionService
{
    private readonly UserManager<Entities.ApplicationUser> _userManager;

    public PermissionService(UserManager<Entities.ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(StaffRoleNames.Admin))
        {
            return PermissionCodes.All;
        }

        return [];
    }

    public async Task<bool> UserHasPermissionAsync(
        int userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsForUserAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }
}
