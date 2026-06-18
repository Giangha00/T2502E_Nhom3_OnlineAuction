using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;

namespace OnlineAuction.Data.Seeders;

public static class AuctionCatalogSeeder
{
    private static readonly string[] LegacySeedEventNames =
    [
        SpreadsheetAuctionCatalog.TestAuctionEventName,
        "RareCard Vault Daily Auctions"
    ];

    public static async Task SeedAsync(AuctionHouseDbContext dbContext, bool refreshInDevelopment = false)
    {
        if (refreshInDevelopment)
        {
            await ClearSeededAuctionsAsync(dbContext);
        }
        else if (await dbContext.Auctions.AnyAsync())
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
        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in SpreadsheetAuctionCatalog.GetEntries())
        {
            var category = await GetOrCreateCategoryAsync(dbContext, entry.CategoryName, categoryCache);
            var bidStep = SpreadsheetAuctionCatalog.ComputeBidStep(entry.StartingPrice);

            var product = new Product
            {
                SellerId = seller.Id,
                CategoryId = category.Id,
                Name = entry.Name,
                ShortDescription = SpreadsheetAuctionCatalog.BuildShortDescription(entry.Description),
                DescriptionHtml = SpreadsheetAuctionCatalog.BuildDescriptionHtml(entry.Description),
                Condition = entry.Condition,
                Year = entry.Year,
                SetName = entry.SetName,
                Language = entry.Language,
                CardNumber = entry.CardNumber,
                GradeLabel = entry.GradeLabel,
                CertNumber = entry.Condition == "graded"
                    ? $"{entry.GradeLabel.Replace(" ", "-")}-{entry.CardNumber.Replace("/", "-")}"
                    : null,
                PrimaryImage = entry.PrimaryImage,
                Category = category,
                CreatedAt = now
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            var startDate = DateTimeUtilities.AsUtc(now.AddSeconds(-30));
            var endDate = DateTimeUtilities.AsUtc(now.AddMinutes(entry.EndMinutes));

            var auction = new Auction
            {
                ProductId = product.Id,
                StartingPrice = entry.StartingPrice,
                BidStep = bidStep,
                CurrentPrice = entry.StartingPrice,
                Status = AuctionStatuses.Live,
                ListingType = ListingTypes.Auction,
                AuctionEventName = SpreadsheetAuctionCatalog.TestAuctionEventName,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = now
            };

            dbContext.Auctions.Add(auction);
            await dbContext.SaveChangesAsync();

            if (bidder is not null && entry.ExistingBidCount > 0)
            {
                var bids = new List<Bid>();
                var amount = entry.StartingPrice;

                for (var i = 0; i < entry.ExistingBidCount; i++)
                {
                    amount += bidStep;
                    bids.Add(new Bid
                    {
                        AuctionId = auction.Id,
                        BidderId = bidder.Id,
                        Amount = amount,
                        IsWinning = i == entry.ExistingBidCount - 1,
                        PlacedAt = now.AddMinutes(-(entry.ExistingBidCount - i))
                    });
                }

                auction.CurrentPrice = amount;
                dbContext.Bids.AddRange(bids);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private static async Task ClearSeededAuctionsAsync(AuctionHouseDbContext dbContext)
    {
        var testAuctions = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.AuctionEventName != null &&
                LegacySeedEventNames.Contains(auction.AuctionEventName))
            .Select(auction => new { auction.Id, auction.ProductId })
            .ToListAsync();

        if (testAuctions.Count == 0)
        {
            return;
        }

        var auctionIds = testAuctions.Select(auction => auction.Id).ToList();
        var productIds = testAuctions.Select(auction => auction.ProductId).Distinct().ToList();

        var orderIds = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item => auctionIds.Contains(item.AuctionId))
            .Select(item => item.OrderId)
            .Distinct()
            .ToListAsync();

        if (orderIds.Count > 0)
        {
            await dbContext.Payments
                .Where(payment => orderIds.Contains(payment.OrderId))
                .ExecuteDeleteAsync();

            await dbContext.OrderItems
                .Where(item => auctionIds.Contains(item.AuctionId))
                .ExecuteDeleteAsync();

            await dbContext.Orders
                .Where(order => orderIds.Contains(order.Id))
                .ExecuteDeleteAsync();
        }

        await dbContext.Bids
            .Where(bid => auctionIds.Contains(bid.AuctionId))
            .ExecuteDeleteAsync();

        await dbContext.AuctionRegistrations
            .Where(registration => auctionIds.Contains(registration.AuctionId))
            .ExecuteDeleteAsync();

        await dbContext.Auctions
            .Where(auction => auctionIds.Contains(auction.Id))
            .ExecuteDeleteAsync();

        await dbContext.ProductImages
            .Where(image => productIds.Contains(image.ProductId))
            .ExecuteDeleteAsync();

        await dbContext.ProductDocuments
            .Where(document => productIds.Contains(document.ProductId))
            .ExecuteDeleteAsync();

        await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .Where(product => !product.Auctions.Any())
            .ExecuteDeleteAsync();
    }

    private static async Task<Category> GetOrCreateCategoryAsync(
        AuctionHouseDbContext dbContext,
        string categoryName,
        Dictionary<string, Category> cache)
    {
        if (cache.TryGetValue(categoryName, out var cached))
        {
            return cached;
        }

        var slug = BuildSlug(categoryName);
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(item => item.Name == categoryName || item.Slug == slug);

        if (category is null)
        {
            category = new Category
            {
                Name = categoryName,
                Slug = slug,
                IsActive = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        cache[categoryName] = category;
        return category;
    }

    private static string BuildSlug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join("-", new string(chars)
            .Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(slug) ? "uncategorized" : slug;
    }
}
