using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class OrderFlowDemoSeeder
{
    private const string DemoCertNumber = "DEMO-ORDER-FLOW-001";

    public static async Task<int?> SeedAsync(AuctionHouseDbContext dbContext)
    {
        var existingAuctionId = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction => auction.Product.CertNumber == DemoCertNumber)
            .Select(auction => (int?)auction.Id)
            .FirstOrDefaultAsync();

        if (existingAuctionId.HasValue)
        {
            return existingAuctionId;
        }

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Status == UserStatus.Active)
            .OrderBy(user => user.Id)
            .Take(2)
            .ToListAsync();

        if (users.Count < 2)
        {
            return null;
        }

        var seller = users[0];
        var buyer = users[1];
        var now = DateTime.UtcNow;

        var category = await dbContext.Categories.FirstOrDefaultAsync(item => item.Slug == "pokemon");
        if (category is null)
        {
            category = new Category
            {
                Name = "Pokemon",
                Slug = "pokemon",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = now
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        var product = new Product
        {
            SellerId = seller.Id,
            CategoryId = category.Id,
            Name = "Demo Won Auction Test Card",
            ShortDescription = "Development seed item for testing pending payment order flow.",
            DescriptionHtml = "<p>Seeded demo item for testing database-backed order checkout.</p>",
            Condition = "graded",
            Year = 2026,
            SetName = "Demo Set",
            GradeLabel = "PSA 10",
            CertNumber = DemoCertNumber,
            PrimaryImage = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop",
            CreatedAt = now
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var auction = new Auction
        {
            ProductId = product.Id,
            StartingPrice = 100m,
            BidStep = 25m,
            CurrentPrice = 350m,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = false,
            Status = AuctionStatuses.Live,
            StartDate = now.AddDays(-3),
            EndDate = now.AddMinutes(-5),
            CreatedAt = now.AddDays(-3)
        };

        dbContext.Auctions.Add(auction);
        await dbContext.SaveChangesAsync();

        dbContext.Bids.AddRange(
            new Bid
            {
                AuctionId = auction.Id,
                BidderId = buyer.Id,
                Amount = 250m,
                BidType = BidTypes.Manual,
                IsWinning = false,
                PlacedAt = now.AddHours(-3),
                CreatedAt = now.AddHours(-3)
            },
            new Bid
            {
                AuctionId = auction.Id,
                BidderId = buyer.Id,
                Amount = 350m,
                BidType = BidTypes.Manual,
                IsWinning = true,
                PlacedAt = now.AddHours(-1),
                CreatedAt = now.AddHours(-1)
            });

        await dbContext.SaveChangesAsync();

        return auction.Id;
    }
}
