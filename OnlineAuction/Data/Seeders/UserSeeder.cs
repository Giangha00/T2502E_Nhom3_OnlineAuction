using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class UserSeeder
{
    private const int SeedUserCount = 150;

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var faker = new Faker("en");

        for (var i = 1; i <= SeedUserCount; i++)
        {
            var fullName = faker.Name.FullName();
            var email = $"user{i}@auctionhouse.local";
            var username = $"user{i}";

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                PhoneNumber = faker.Phone.PhoneNumber("09########"),
                Role = i % 12 == 0 ? UserRole.Admin : UserRole.User,
                Status = i % 4 == 0 ? UserStatus.Inactive : UserStatus.Active,
                AvatarUrl = $"/admin/images/user/user-{((i - 1) % 37) + 1:D2}.jpg",
                CreatedAt = faker.Date.Past(2, DateTime.UtcNow)
            };

            await userManager.CreateAsync(user, "User@123");
        }
    }
}
