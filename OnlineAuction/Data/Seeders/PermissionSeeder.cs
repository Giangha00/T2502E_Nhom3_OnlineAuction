using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        await MigrateLegacyStaffRolesAsync(dbContext, userManager);

        if (!await roleManager.RoleExistsAsync(StaffRoleNames.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = StaffRoleNames.Admin });
        }

        await SyncExistingUserRolesAsync(dbContext, userManager);
    }

    private static async Task MigrateLegacyStaffRolesAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE users SET role = {0} WHERE role IN (3, 4)",
            (int)UserRole.User);

        var legacyStaffEmails = new[] { "moderator@auctionhouse.com", "support@auctionhouse.com" };
        foreach (var email in legacyStaffEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                continue;
            }

            user.Role = UserRole.User;
            await userManager.UpdateAsync(user);
            await IdentityRoleSyncService.SyncUserRoleAsync(userManager, user, UserRole.User);
        }
    }

    private static async Task SyncExistingUserRolesAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        var users = await dbContext.Users.ToListAsync();
        foreach (var user in users)
        {
            await IdentityRoleSyncService.SyncUserRoleAsync(userManager, user, user.Role);
        }
    }
}
