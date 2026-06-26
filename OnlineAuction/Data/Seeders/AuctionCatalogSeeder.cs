using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;

namespace OnlineAuction.Data.Seeders;

public static class AuctionCatalogSeeder
{
    private static readonly string[] LegacySeedEventNames =
    [
        SpreadsheetAuctionCatalog.TestAuctionEventName,
        "RareCard Vault Buy Now",
        "RareCard Vault Daily Auctions"
    ];

    private const int SeededAuctionDurationDays = 7;

    public static async Task SeedAsync(AuctionHouseDbContext dbContext, bool refreshInDevelopment = false)
    {
        if (refreshInDevelopment)
        {
            await ClearSeededAuctionsAsync(dbContext);
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

        var existingProductNames = await GetExistingSeededProductNamesAsync(dbContext);
        var now = DateTime.UtcNow;
        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in SpreadsheetAuctionCatalog.GetEntries())
        {
            if (existingProductNames.Contains(entry.Name))
            {
                continue;
            }

            await SeedEntryAsync(dbContext, entry, seller.Id, bidder?.Id, categoryCache, now);
            existingProductNames.Add(entry.Name);
        }

        await BackfillBuyNowPricesAsync(dbContext);

        if (!refreshInDevelopment)
        {
            await ReactivateExpiredSeededListingsAsync(dbContext, now);
        }
    }

    private static async Task<HashSet<string>> GetExistingSeededProductNamesAsync(AuctionHouseDbContext dbContext)
    {
        var names = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Auctions.Any(auction =>
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
            .Select(product => product.Name)
            .ToListAsync();

        return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SeedEntryAsync(
        AuctionHouseDbContext dbContext,
        SpreadsheetAuctionCatalog.Entry entry,
        int sellerId,
        int? bidderId,
        Dictionary<string, Category> categoryCache,
        DateTime now)
    {
        var category = await GetOrCreateCategoryAsync(dbContext, entry.CategoryName, categoryCache);
        var bidStep = SpreadsheetAuctionCatalog.ComputeBidStep(entry.StartingPrice);
        var buyNowPrice = SpreadsheetAuctionCatalog.TryGetBuyNowPrice(entry.Name);

        var product = new Product
        {
            SellerId = sellerId,
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

        product.ProductNumber = $"PRD-{product.Id:D8}";
        var template = await ProductTemplateSync.ResolveTemplateForProductAsync(dbContext, product, null);
        product.ProductTemplateId = template.Id;
        product.Price ??= entry.StartingPrice;
        product.Quantity = 1;
        await dbContext.SaveChangesAsync();

        var startDate = DateTimeUtilities.AsUtc(now.AddSeconds(-30));
        var endDate = DateTimeUtilities.AsUtc(now.AddDays(SeededAuctionDurationDays));

        var auction = new Auction
        {
            ProductId = product.Id,
            StartingPrice = entry.StartingPrice,
            BidStep = bidStep,
            CurrentPrice = entry.StartingPrice,
            BuyNowPrice = buyNowPrice,
            Status = AuctionStatuses.Live,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = true,
            AuctionEventName = SpreadsheetAuctionCatalog.TestAuctionEventName,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = now
        };

        dbContext.Auctions.Add(auction);
        await dbContext.SaveChangesAsync();

        if (bidderId is null || entry.ExistingBidCount <= 0)
        {
            return;
        }

        var bids = new List<Bid>();
        var amount = entry.StartingPrice;

        for (var i = 0; i < entry.ExistingBidCount; i++)
        {
            amount += bidStep;
            bids.Add(new Bid
            {
                AuctionId = auction.Id,
                BidderId = bidderId.Value,
                Amount = amount,
                IsWinning = i == entry.ExistingBidCount - 1,
                PlacedAt = now.AddMinutes(-(entry.ExistingBidCount - i))
            });
        }

        auction.CurrentPrice = amount;
        dbContext.Bids.AddRange(bids);
        await dbContext.SaveChangesAsync();
    }

    private static async Task BackfillBuyNowPricesAsync(AuctionHouseDbContext dbContext)
    {
        var priceMap = SpreadsheetAuctionCatalog.GetBuyNowPriceMap();
        if (priceMap.Count == 0)
        {
            return;
        }

        var productNames = priceMap.Keys.ToList();
        var auctions = await dbContext.Auctions
            .Include(auction => auction.Product)
            .Where(auction =>
                auction.BuyNowPrice == null &&
                auction.Product.Name != null &&
                productNames.Contains(auction.Product.Name))
            .ToListAsync();

        if (auctions.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var auction in auctions)
        {
            if (priceMap.TryGetValue(auction.Product.Name, out var buyNowPrice))
            {
                auction.BuyNowPrice = buyNowPrice;
                auction.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task ReactivateExpiredSeededListingsAsync(AuctionHouseDbContext dbContext, DateTime now)
    {
        var seededListings = await dbContext.Auctions
            .Where(auction =>
                auction.AuctionEventName != null &&
                LegacySeedEventNames.Contains(auction.AuctionEventName))
            .ToListAsync();

        if (seededListings.Count == 0)
        {
            return;
        }

        var changed = false;

        foreach (var auction in seededListings)
        {
            var shouldReactivate = auction.EndDate <= now
                || auction.Status is AuctionStatuses.Ended
                    or AuctionStatuses.AwaitingPayment
                    or AuctionStatuses.Completed;

            if (!shouldReactivate)
            {
                continue;
            }

            auction.Status = AuctionStatuses.Live;
            auction.StartDate = DateTimeUtilities.AsUtc(now.AddSeconds(-30));
            auction.EndDate = DateTimeUtilities.AsUtc(now.AddDays(SeededAuctionDurationDays));
            auction.WinnerId = null;
            auction.UpdatedAt = now;
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync();
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

        var slug = CategorySlug.ToSlug(categoryName);
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
}
