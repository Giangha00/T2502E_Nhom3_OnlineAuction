using Microsoft.AspNetCore.Identity;
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

    private const string OnePieceSellerEmail = "nguyen.hai@auctionhouse.local";
    private const string YugiohSellerEmail = "nguyen.ha@auctionhouse.local";
    private const string DemoPassword = "User@123";

    private static bool IsExpiredSeededListing(Auction auction, DateTime now) =>
        auction.EndDate <= now
        || auction.Status is AuctionStatuses.Ended
            or AuctionStatuses.AwaitingPayment
            or AuctionStatuses.Completed;

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        bool refreshInDevelopment = false)
    {
        if (refreshInDevelopment)
        {
            await ClearSeededAuctionsAsync(dbContext);
        }

        var onePieceSeller = await EnsureSellerAsync(
            userManager,
            OnePieceSellerEmail,
            "nguyen.hai",
            "Nguyễn Hải");

        var yugiohSeller = await EnsureSellerAsync(
            userManager,
            YugiohSellerEmail,
            "nguyen.ha",
            "Nguyễn Hà");

        var bidder = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user3@auctionhouse.local" && u.Status == UserStatus.Active);

        if (onePieceSeller is null || yugiohSeller is null)
        {
            return;
        }

        var existingProductNames = await GetExistingSeededProductNamesAsync(dbContext);
        var now = DateTime.UtcNow;
        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        var seedIndex = 0;
        foreach (var entry in SpreadsheetAuctionCatalog.GetEntries())
        {
            if (existingProductNames.Contains(entry.Name))
            {
                continue;
            }

            var sellerId = ResolveSellerId(entry.CategoryName, onePieceSeller.Id, yugiohSeller.Id);
            await SeedEntryAsync(dbContext, entry, sellerId, bidder?.Id, categoryCache, now, seedIndex);
            existingProductNames.Add(entry.Name);
            seedIndex++;
        }

        await BackfillBuyNowPricesAsync(dbContext);
        await EnsureFullFlowDemoScheduleAsync(dbContext, DateTime.UtcNow);
        await DeactivateUnusedSeedCategoriesAsync(dbContext);

        if (!refreshInDevelopment)
        {
            await ReactivateExpiredSeededListingsAsync(dbContext, now);
        }
    }

    private static async Task DeactivateUnusedSeedCategoriesAsync(AuctionHouseDbContext dbContext)
    {
        var keep = new[] { "One Piece", "Yu-Gi-Oh!" };
        var extras = await dbContext.Categories
            .Where(category => category.IsActive && !keep.Contains(category.Name))
            .ToListAsync();

        if (extras.Count == 0)
        {
            return;
        }

        foreach (var category in extras)
        {
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static int ResolveSellerId(string categoryName, int onePieceSellerId, int yugiohSellerId) =>
        categoryName.Equals("Yu-Gi-Oh!", StringComparison.OrdinalIgnoreCase)
            ? yugiohSellerId
            : onePieceSellerId;

    private static async Task<ApplicationUser?> EnsureSellerAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string userName,
        string fullName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                PhoneNumber = "0900000000",
                Role = UserRole.User,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                AvatarUrl = "/admin/images/user/user-01.jpg",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                return null;
            }

            return user;
        }

        var needsUpdate = false;
        if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
        {
            user.FullName = fullName;
            needsUpdate = true;
        }

        if (user.Status != UserStatus.Active)
        {
            user.Status = UserStatus.Active;
            needsUpdate = true;
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await userManager.UpdateAsync(user);
        }

        return user;
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
        DateTime now,
        int seedIndex)
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

        var gallery = new List<ProductImage>
        {
            new()
            {
                ProductId = product.Id,
                ImageUrl = entry.PrimaryImage,
                SortOrder = 0,
                CreatedAt = now
            }
        };

        if (entry.GalleryImages is { Count: > 0 })
        {
            var sort = 1;
            foreach (var url in entry.GalleryImages)
            {
                if (string.IsNullOrWhiteSpace(url) ||
                    url.Equals(entry.PrimaryImage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                gallery.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = url,
                    SortOrder = sort++,
                    CreatedAt = now
                });
            }
        }

        dbContext.ProductImages.AddRange(gallery);
        await dbContext.SaveChangesAsync();

        var auction = new Auction
        {
            ProductId = product.Id,
            StartingPrice = entry.StartingPrice,
            BidStep = bidStep,
            CurrentPrice = entry.StartingPrice,
            BuyNowPrice = buyNowPrice,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = true,
            AuctionEventName = SpreadsheetAuctionCatalog.TestAuctionEventName,
            CreatedAt = now
        };

        if (SpreadsheetAuctionCatalog.IsFullFlowDemoProduct(entry.Name))
        {
            AuctionScheduleHelper.ApplyFullFlowDemoSchedule(auction, now);
        }
        else
        {
            AuctionScheduleHelper.ApplyTestAuctionSchedule(auction, seedIndex, now);
        }

        dbContext.Auctions.Add(auction);
        await dbContext.SaveChangesAsync();

        var isLivePhase = seedIndex % 4 is 2 or 3
            && !SpreadsheetAuctionCatalog.IsFullFlowDemoProduct(entry.Name);
        if (bidderId is null || entry.ExistingBidCount <= 0 || !isLivePhase)
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

    private static async Task EnsureFullFlowDemoScheduleAsync(AuctionHouseDbContext dbContext, DateTime now)
    {
        var demoProductName = SpreadsheetAuctionCatalog.FullFlowDemoProductName;

        var auction = await dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a =>
                a.DeletedAt == null &&
                a.AuctionEventName == SpreadsheetAuctionCatalog.TestAuctionEventName &&
                a.Product.DeletedAt == null &&
                a.Product.Name != null &&
                (a.Product.Name == demoProductName || a.Product.Name.StartsWith(demoProductName)));

        if (auction is null)
        {
            return;
        }

        AuctionScheduleHelper.ApplyFullFlowDemoSchedule(auction, now);
        auction.UpdatedAt = now;
        await dbContext.SaveChangesAsync();
    }

    private static async Task ReactivateExpiredSeededListingsAsync(AuctionHouseDbContext dbContext, DateTime now)
    {
        var seededListings = await dbContext.Auctions
            .Include(auction => auction.Product)
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
            var shouldReactivate = IsExpiredSeededListing(auction, now);

            if (!shouldReactivate)
            {
                continue;
            }

            if (SpreadsheetAuctionCatalog.IsFullFlowDemoProduct(auction.Product?.Name))
            {
                AuctionScheduleHelper.ApplyFullFlowDemoSchedule(auction, now);
            }
            else
            {
                AuctionScheduleHelper.ApplyTestAuctionSchedule(auction, auction.Id, now);
            }

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
                SortOrder = categoryName.Equals("One Piece", StringComparison.OrdinalIgnoreCase) ? 1 : 2,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        cache[categoryName] = category;
        return category;
    }
}
