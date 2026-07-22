using Microsoft.AspNetCore.Identity;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class IdentityRoleSyncService
{
    public static string? MapUserRoleToIdentityRole(UserRole role) => role switch
    {
        UserRole.Admin => StaffRoleNames.Admin,
        _ => null
    };

    public static async Task SyncUserRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        UserRole role)
    {
        var currentRoles = await userManager.GetRolesAsync(user);
        foreach (var staffRole in currentRoles.Where(StaffRoleNames.All.Contains))
        {
            await userManager.RemoveFromRoleAsync(user, staffRole);
        }

        var identityRole = MapUserRoleToIdentityRole(role);
        if (identityRole is not null && !await userManager.IsInRoleAsync(user, identityRole))
        {
            await userManager.AddToRoleAsync(user, identityRole);
        }
    }

    public static async Task<bool> HasAdminAccessAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        if (user.Role == UserRole.Admin || await userManager.IsInRoleAsync(user, StaffRoleNames.Admin))
        {
            return true;
        }

        var claims = await userManager.GetClaimsAsync(user);
        return claims.Any(claim =>
            claim.Type == PermissionClaimTypes.Permission
            && !string.IsNullOrWhiteSpace(claim.Value)
            && PermissionCatalog.IsKnown(claim.Value));
    }
}
