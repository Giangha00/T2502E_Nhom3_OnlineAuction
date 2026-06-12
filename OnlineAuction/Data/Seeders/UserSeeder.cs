using Bogus;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class UserSeeder
{
    private const int SeedUserCount = 150;

    public static async Task SeedAsync(AuctionHouseDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var faker = new Faker("en");
        var users = new List<User>();

        for (var i = 1; i <= SeedUserCount; i++)
        {
            var fullName = faker.Name.FullName();

            users.Add(new User
            {
                FullName = fullName,
                Email = $"user{i}@auctionhouse.local",
                PhoneNumber = faker.Phone.PhoneNumber("09########"),
                Role = i % 12 == 0 ? UserRole.Admin : UserRole.User,
                Status = GetRandomStatus(i),
                Gender = GetRandomGender(i),
                AvatarUrl = $"/admin/images/user/user-{((i - 1) % 37) + 1:D2}.jpg",
                InitialPassword = "User@123",
                AuctionCount = faker.Random.Int(0, 25),
                HasActiveAuctionOrTransaction = i % 9 == 0,
                CreatedDate = faker.Date.Past(2, DateTime.UtcNow),
                UpdatedDate = null
            });
        }

        await dbContext.Users.AddRangeAsync(users);
        await dbContext.SaveChangesAsync();
    }

    private static UserStatus GetRandomStatus(int index)
    {
        if (index % 10 == 0)
        {
            return UserStatus.Blocked;
        }

        if (index % 4 == 0)
        {
            return UserStatus.Inactive;
        }

        return UserStatus.Active;
    }

    private static Gender GetRandomGender(int index)
    {
        return index % 2 == 0 ? Gender.Male : Gender.Female;
    }
}
