using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
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

    public static Task<bool> HasAdminAccessAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        AuctionHouseDbContext dbContext)
    {
        if (user.Role == UserRole.Admin)
        {
            return Task.FromResult(true);
        }

        return dbContext.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == user.Id);
    }
}
