using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class AdminSeeder
{
    private const string AdminRoleName = "Admin";
    private const string AdminEmail = "admin@auctionhouse.com";
    private const string AdminPassword = "User@123";

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = AdminRoleName });
        }

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = AdminEmail,
                FullName = "System Administrator",
                PhoneNumber = "0900000000",
                Role = UserRole.Admin,
                IsSuperAdmin = true,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
        {
            await userManager.AddToRoleAsync(adminUser, AdminRoleName);
        }

        if (adminUser.Role != UserRole.Admin || !adminUser.EmailConfirmed || !adminUser.IsSuperAdmin)
        {
            adminUser.Role = UserRole.Admin;
            adminUser.IsSuperAdmin = true;
            adminUser.Status = UserStatus.Active;
            adminUser.EmailConfirmed = true;
            await userManager.UpdateAsync(adminUser);
        }

        var adminUsersWithoutRole = await dbContext.Users
            .Where(user => user.Role == UserRole.Admin)
            .ToListAsync();

        foreach (var user in adminUsersWithoutRole)
        {
            if (!await userManager.IsInRoleAsync(user, AdminRoleName))
            {
                await userManager.AddToRoleAsync(user, AdminRoleName);
            }
        }
    }
}
