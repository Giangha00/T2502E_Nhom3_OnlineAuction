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

    private static readonly Dictionary<string, string[]> RolePermissionMap = new(StringComparer.Ordinal)
    {
        [StaffRoleNames.Moderator] =
        [
            PermissionCodes.DashboardView,
            PermissionCodes.AuctionsView,
            PermissionCodes.AuctionsVerify
        ],
        [StaffRoleNames.Support] =
        [
            PermissionCodes.DashboardView,
            PermissionCodes.UsersView,
            PermissionCodes.ComplaintsReview
        ]
    };

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
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

        foreach (var roleName in StaffRoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });
            }
        }

        var permissionsByCode = await dbContext.Permissions
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);

        foreach (var (roleName, permissionCodes) in RolePermissionMap)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var desiredPermissionIds = permissionCodes
                .Where(permissionsByCode.ContainsKey)
                .Select(code => permissionsByCode[code])
                .ToHashSet();

            var existing = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();

            foreach (var permissionId in desiredPermissionIds.Where(id => !existingIds.Contains(id)))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
            }
        }

        await dbContext.SaveChangesAsync();

        await SeedDemoStaffUsersAsync(dbContext, userManager);
        await SyncExistingUserRolesAsync(dbContext, userManager);
    }

    private static async Task SeedDemoStaffUsersAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        await EnsureStaffUserAsync(
            userManager,
            email: "moderator@auctionhouse.com",
            username: "moderator",
            fullName: "Demo Moderator",
            role: UserRole.Moderator,
            identityRole: StaffRoleNames.Moderator);

        await EnsureStaffUserAsync(
            userManager,
            email: "support@auctionhouse.com",
            username: "support",
            fullName: "Demo Support",
            role: UserRole.Support,
            identityRole: StaffRoleNames.Support);
    }

    private static async Task EnsureStaffUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string username,
        string fullName,
        UserRole role,
        string identityRole)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                PhoneNumber = "0900000001",
                Role = role,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, "User@123");
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        await IdentityRoleSyncService.SyncUserRoleAsync(userManager, user, role);
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
