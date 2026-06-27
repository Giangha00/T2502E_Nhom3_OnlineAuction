using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class PermissionSeeder
{
    private sealed record PermissionSeed(string Code, string Name, string Module, string? Description);

    private static readonly PermissionSeed[] PermissionCatalog =
    [
        new(PermissionCodes.DashboardView, "View Dashboard", "Dashboard", "Access admin dashboard and exports"),
        new(PermissionCodes.AuctionsView, "View Auctions", "Auctions", "View auction list and details"),
        new(PermissionCodes.AuctionsManage, "Manage Auctions", "Auctions", "Create, edit, and delete auctions"),
        new(PermissionCodes.AuctionsVerify, "Verify Auctions", "Auctions", "Approve or reject seller listings"),
        new(PermissionCodes.UsersView, "View Users", "Users", "View user list and profiles"),
        new(PermissionCodes.UsersManage, "Manage Users", "Users", "Create, edit, delete users and manage roles"),
        new(PermissionCodes.CategoriesManage, "Manage Categories", "Categories", "Full category CRUD"),
        new(PermissionCodes.ProductsManage, "Manage Products", "Products", "Product admin module (backlog)"),
        new(PermissionCodes.ComplaintsReview, "Review Complaints", "Complaints", "Complaint review module (backlog)")
    ];

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        await MigrateLegacyStaffRolesAsync(dbContext, userManager);

        foreach (var seed in PermissionCatalog)
        {
            var exists = await dbContext.Permissions.AnyAsync(permission => permission.Code == seed.Code);
            if (exists)
            {
                continue;
            }

            dbContext.Permissions.Add(new Permission
            {
                Code = seed.Code,
                Name = seed.Name,
                Module = seed.Module,
                Description = seed.Description
            });
        }

        await dbContext.SaveChangesAsync();

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
