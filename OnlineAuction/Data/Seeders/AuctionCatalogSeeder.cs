using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Data.Seeders;

public static class AuctionCatalogSeeder
{
    public static async Task SeedAsync(AuctionHouseDbContext dbContext)
    {
        if (await dbContext.Auctions.AnyAsync())
        {
            return;
        }

        var seller = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user1@auctionhouse.local" && u.Status == UserStatus.Active);

        var bidder = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user3@auctionhouse.local" && u.Status == UserStatus.Active);

        if (seller is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var catalog = new (Product Product, decimal StartingPrice, decimal BidStep, decimal CurrentPrice, string Status, int EndDays, int EndHours)[]
        {
            (
                new Product
                {
                    SellerId = seller.Id,
                    Name = "Charizard 1st Edition Holo",
                    Category = "Pokémon",
                    ShortDescription = "Authenticated Pokémon holo graded and vault-ready.",
                    DescriptionHtml = "<p>1999 Base Set Charizard holo offered through RareCard Vault with documented provenance.</p>",
                    Condition = "graded",
                    Year = 1999,
                    SetName = "Base Set",
                    GradeLabel = "PSA 10",
                    CertNumber = "PSA-48000137",
                    PrimaryImage = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop",
                    CreatedAt = now
                },
                85000, 500, 124500, AuctionStatuses.Live, 2, 14
            ),
            (
                new Product
                {
                    SellerId = seller.Id,
                    Name = "Gear 5 Luffy Manga Rare",
                    Category = "One Piece",
                    ShortDescription = "Premium One Piece TCG manga rare parallel.",
                    Condition = "graded",
                    Year = 2023,
                    SetName = "OP-05 Awakening",
                    GradeLabel = "BGS 9.5",
                    CertNumber = "BGS-00123456",
                    PrimaryImage = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop",
                    CreatedAt = now
                },
                4200, 50, 6800, AuctionStatuses.EndingSoon, 0, 5
            ),
            (
                new Product
                {
                    SellerId = seller.Id,
                    Name = "Blue-Eyes White Dragon LOB",
                    Category = "Yu-Gi-Oh!",
                    ShortDescription = "Legend of Blue Eyes White Dragon graded collectible.",
                    Condition = "graded",
                    Year = 2002,
                    SetName = "Legend of Blue Eyes",
                    GradeLabel = "PSA 10",
                    CertNumber = "PSA-48000274",
                    PrimaryImage = "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=600&h=750&fit=crop",
                    CreatedAt = now
                },
                12000, 100, 18500, AuctionStatuses.Live, 1, 8
            ),
            (
                new Product
                {
                    SellerId = seller.Id,
                    Name = "LeBron James Topps Chrome RC",
                    Category = "Sports",
                    ShortDescription = "Investment-grade sports card with verified provenance.",
                    Condition = "graded",
                    Year = 2003,
                    SetName = "Topps Chrome",
                    GradeLabel = "PSA 10",
                    CertNumber = "PSA-48000411",
                    PrimaryImage = "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=600&h=750&fit=crop",
                    CreatedAt = now
                },
                98000, 500, 142000, AuctionStatuses.Live, 3, 2
            ),
            (
                new Product
                {
                    SellerId = seller.Id,
                    Name = "Black Lotus Alpha",
                    Category = "Magic: The Gathering",
                    ShortDescription = "Alpha Black Lotus authenticated for collectors.",
                    Condition = "graded",
                    Year = 1993,
                    SetName = "Alpha Edition",
                    GradeLabel = "CGC 8.5",
                    CertNumber = "CGC-50012345",
                    PrimaryImage = "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=600&h=750&fit=crop",
                    CreatedAt = now
                },
                180000, 1000, 245000, AuctionStatuses.EndingSoon, 0, 1
            )
        };

        foreach (var entry in catalog)
        {
            dbContext.Products.Add(entry.Product);
            await dbContext.SaveChangesAsync();

            var auction = new Auction
            {
                ProductId = entry.Product.Id,
                StartingPrice = entry.StartingPrice,
                BidStep = entry.BidStep,
                CurrentPrice = entry.CurrentPrice,
                Status = entry.Status,
                StartDate = now.AddDays(-7),
                EndDate = now.AddDays(entry.EndDays).AddHours(entry.EndHours),
                CreatedAt = now
            };

            dbContext.Auctions.Add(auction);
            await dbContext.SaveChangesAsync();

            if (bidder is not null && entry.CurrentPrice > entry.StartingPrice)
            {
                dbContext.Bids.AddRange(
                    new Bid
                    {
                        AuctionId = auction.Id,
                        BidderId = bidder.Id,
                        Amount = entry.StartingPrice + entry.BidStep,
                        IsWinning = false,
                        PlacedAt = now.AddHours(-6)
                    },
                    new Bid
                    {
                        AuctionId = auction.Id,
                        BidderId = bidder.Id,
                        Amount = entry.CurrentPrice,
                        IsWinning = true,
                        PlacedAt = now.AddHours(-1)
                    });

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
