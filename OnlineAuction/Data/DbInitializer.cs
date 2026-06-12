using OnlineAuction.Models;
using Microsoft.AspNetCore.Identity;

namespace OnlineAuction.Data;

public static class DbInitializer
{
    public static async Task SeedRolesAndAdminAsync(
        IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(
                new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(
                new IdentityRole("User"));
        }
        var admin =
            await userManager.FindByEmailAsync(
                "admin@gmail.com");

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@gmail.com",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(
                admin,
                "Admin123@");
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                "Admin"))
        {
            await userManager.AddToRoleAsync(
                admin,
                "Admin");
        }
        
    }
}