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
    private const string SportsSellerEmail = "viet.anh@auctionhouse.local";
    private const string DemoPassword = "User@123";

    private static bool IsExpiredSeededListing(Auction auction, DateTime now) =>
        auction.EndDate <= now
        || auction.Status is AuctionStatuses.Ended
            or AuctionStatuses.AwaitingPayment
            or AuctionStatuses.Completed;

    public static async Task SeedAsync(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        bool refreshInDevelopment = false,
        bool syncCatalog = false)
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

        var sportsSeller = await EnsureSellerAsync(
            userManager,
            SportsSellerEmail,
            "viet.anh",
            "Việt Anh");

        var bidder = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user3@auctionhouse.local" && u.Status == UserStatus.Active);

        if (onePieceSeller is null || yugiohSeller is null)
        {
            return;
        }

        var sportsSellerId = sportsSeller?.Id;

        var existingProducts = refreshInDevelopment || !syncCatalog
            ? new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
            : await GetExistingSeededProductsByNameAsync(dbContext);
        var now = DateTime.UtcNow;
        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var templateCache = new Dictionary<string, ProductTemplate>(StringComparer.OrdinalIgnoreCase);
        var catalogNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seedIndex = 0;
        foreach (var entry in SpreadsheetAuctionCatalog.GetEntries())
        {
            catalogNames.Add(entry.Name);

            var sellerId = ResolveSellerId(
                entry.CategoryName,
                onePieceSeller.Id,
                yugiohSeller.Id,
                sportsSellerId);
            if (!sellerId.HasValue)
            {
                continue;
            }

            if (existingProducts.TryGetValue(entry.Name, out var existingProduct))
            {
                await SyncSeededEntryAsync(
                    dbContext,
                    existingProduct,
                    entry,
                    sellerId.Value,
                    categoryCache,
                    templateCache,
                    now);
                seedIndex++;
                continue;
            }

            await SeedEntryAsync(dbContext, entry, sellerId.Value, bidder?.Id, categoryCache, templateCache, now, seedIndex);
            seedIndex++;
        }

        if (syncCatalog && !refreshInDevelopment)
        {
            await RemoveOrphanedSeededProductsAsync(dbContext, catalogNames);
        }

        await BackfillSeededProductTemplatesAsync(dbContext, templateCache);
        await SyncBuyNowPricesAsync(dbContext);
        await EnsureFullFlowDemoScheduleAsync(dbContext, DateTime.UtcNow);
        await SyncSeedCategoriesAsync(dbContext);

        if (!refreshInDevelopment)
        {
            await ReactivateExpiredSeededListingsAsync(dbContext, now);
        }
    }

    private static int? ResolveSellerId(
        string categoryName,
        int onePieceSellerId,
        int yugiohSellerId,
        int? sportsSellerId) =>
        categoryName switch
        {
            _ when categoryName.Equals("Yu-Gi-Oh!", StringComparison.OrdinalIgnoreCase) => yugiohSellerId,
            _ when categoryName.Equals("Pokémon", StringComparison.OrdinalIgnoreCase) => yugiohSellerId,
            _ when categoryName.Equals("Sports", StringComparison.OrdinalIgnoreCase) => sportsSellerId,
            _ => onePieceSellerId
        };

    private static async Task SyncSeedCategoriesAsync(AuctionHouseDbContext dbContext)
    {
        var keep = new[] { "One Piece", "Yu-Gi-Oh!", "Pokémon", "Sports" };
        var keepNormalized = keep
            .Select(CategorySlug.NormalizeForCompare)
            .ToHashSet(StringComparer.Ordinal);

        var categories = await dbContext.Categories.ToListAsync();
        var changed = false;
        var now = DateTime.UtcNow;

        foreach (var category in categories)
        {
            var normalized = CategorySlug.NormalizeForCompare(category.Name);
            var shouldKeep = keep.Contains(category.Name, StringComparer.OrdinalIgnoreCase)
                || keepNormalized.Contains(normalized);

            if (shouldKeep && !category.IsActive)
            {
                category.IsActive = true;
                category.UpdatedAt = now;
                changed = true;
            }
            else if (!shouldKeep && category.IsActive)
            {
                category.IsActive = false;
                category.UpdatedAt = now;
                changed = true;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync();
        }
    }

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

    private static async Task<Dictionary<string, Product>> GetExistingSeededProductsByNameAsync(
        AuctionHouseDbContext dbContext)
    {
        var products = await dbContext.Products
            .Include(product => product.Images)
            .Include(product => product.Auctions)
                .ThenInclude(auction => auction.Bids)
            .Include(product => product.Category)
            .Where(product =>
                product.Auctions.Any(auction =>
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
            .ToListAsync();

        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Name))
            .GroupBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SyncSeededEntryAsync(
        AuctionHouseDbContext dbContext,
        Product product,
        SpreadsheetAuctionCatalog.Entry entry,
        int sellerId,
        Dictionary<string, Category> categoryCache,
        Dictionary<string, ProductTemplate> templateCache,
        DateTime now)
    {
        var category = await GetOrCreateCategoryAsync(dbContext, entry.CategoryName, categoryCache);
        var template = await GetOrCreateTemplateFromEntryAsync(dbContext, entry, category, templateCache, now);

        ApplyProductFieldsFromEntry(product, entry, sellerId, category.Id, template.Id);
        product.UpdatedAt = now;

        SyncProductImages(dbContext, product, entry, now);

        foreach (var auction in product.Auctions.Where(IsLegacySeededAuction))
        {
            SyncSeededAuctionFromEntry(auction, entry);
            auction.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private static bool IsLegacySeededAuction(Auction auction) =>
        auction.AuctionEventName != null &&
        LegacySeedEventNames.Contains(auction.AuctionEventName);

    private static void ApplyProductFieldsFromEntry(
        Product product,
        SpreadsheetAuctionCatalog.Entry entry,
        int sellerId,
        int categoryId,
        int templateId)
    {
        product.SellerId = sellerId;
        product.CategoryId = categoryId;
        product.ProductTemplateId = templateId;
        product.Name = entry.Name;
        product.ShortDescription = SpreadsheetAuctionCatalog.BuildShortDescription(entry.Description);
        product.DescriptionHtml = SpreadsheetAuctionCatalog.BuildDescriptionHtml(entry.Description);
        product.Condition = entry.Condition;
        product.Year = entry.Year;
        product.SetName = entry.SetName;
        product.Language = entry.Language;
        product.CardNumber = entry.CardNumber;
        product.GradeLabel = entry.GradeLabel;
        product.CertNumber = entry.Condition == "graded"
            ? $"{entry.GradeLabel.Replace(" ", "-")}-{entry.CardNumber.Replace("/", "-")}"
            : null;
        product.PrimaryImage = entry.PrimaryImage;
    }

    private static void ApplyTemplateFieldsFromEntry(
        ProductTemplate template,
        SpreadsheetAuctionCatalog.Entry entry,
        int categoryId)
    {
        template.Name = entry.Name;
        template.CategoryId = categoryId;
        template.SetName = entry.SetName;
        template.CardNumber = entry.CardNumber;
        template.GradeLabel = entry.GradeLabel;
        template.Year = entry.Year;
        template.Language = entry.Language;
        template.ShortDescription = SpreadsheetAuctionCatalog.BuildShortDescription(entry.Description);
        template.DescriptionHtml = SpreadsheetAuctionCatalog.BuildDescriptionHtml(entry.Description);
        template.PrimaryImage = entry.PrimaryImage;
        template.IsActive = true;
    }

    private static void SyncProductImages(
        AuctionHouseDbContext dbContext,
        Product product,
        SpreadsheetAuctionCatalog.Entry entry,
        DateTime now)
    {
        var desiredUrls = BuildDesiredImageUrls(entry);
        var currentUrls = product.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => image.ImageUrl)
            .ToList();

        if (currentUrls.SequenceEqual(desiredUrls, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (product.Images.Count > 0)
        {
            dbContext.ProductImages.RemoveRange(product.Images);
            product.Images.Clear();
        }

        var sortOrder = 0;
        foreach (var url in desiredUrls)
        {
            var image = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = url,
                SortOrder = sortOrder++,
                CreatedAt = now
            };
            product.Images.Add(image);
            dbContext.ProductImages.Add(image);
        }
    }

    private static List<string> BuildDesiredImageUrls(SpreadsheetAuctionCatalog.Entry entry)
    {
        var urls = new List<string> { entry.PrimaryImage };

        if (entry.GalleryImages is not { Count: > 0 })
        {
            return urls;
        }

        foreach (var url in entry.GalleryImages)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                url.Equals(entry.PrimaryImage, StringComparison.OrdinalIgnoreCase) ||
                urls.Contains(url, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            urls.Add(url);
        }

        return urls;
    }

    private static void SyncSeededAuctionFromEntry(
        Auction auction,
        SpreadsheetAuctionCatalog.Entry entry)
    {
        var bidStep = SpreadsheetAuctionCatalog.ComputeBidStep(entry.StartingPrice);
        var buyNowPrice = SpreadsheetAuctionCatalog.TryGetBuyNowPrice(entry.Name);
        var hasBids = auction.Bids.Count > 0;

        auction.BidStep = bidStep;
        auction.BuyNowPrice = buyNowPrice;

        if (!hasBids)
        {
            auction.StartingPrice = entry.StartingPrice;
            auction.CurrentPrice = entry.StartingPrice;
        }
    }

    private static async Task RemoveOrphanedSeededProductsAsync(
        AuctionHouseDbContext dbContext,
        IReadOnlySet<string> catalogNames)
    {
        var orphanedProductIds = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Name != null &&
                !catalogNames.Contains(product.Name) &&
                product.Auctions.Any(auction =>
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
            .Select(product => product.Id)
            .ToListAsync();

        if (orphanedProductIds.Count == 0)
        {
            return;
        }

        await DeleteSeededProductsAsync(dbContext, orphanedProductIds);
    }

    private static async Task SeedEntryAsync(
        AuctionHouseDbContext dbContext,
        SpreadsheetAuctionCatalog.Entry entry,
        int sellerId,
        int? bidderId,
        Dictionary<string, Category> categoryCache,
        Dictionary<string, ProductTemplate> templateCache,
        DateTime now,
        int seedIndex)
    {
        var category = await GetOrCreateCategoryAsync(dbContext, entry.CategoryName, categoryCache);
        var template = await GetOrCreateTemplateFromEntryAsync(dbContext, entry, category, templateCache, now);
        var bidStep = SpreadsheetAuctionCatalog.ComputeBidStep(entry.StartingPrice);
        var buyNowPrice = SpreadsheetAuctionCatalog.TryGetBuyNowPrice(entry.Name);

        var product = new Product
        {
            Category = category,
            CreatedAt = now
        };

        ApplyProductFieldsFromEntry(product, entry, sellerId, category.Id, template.Id);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        SyncProductImages(dbContext, product, entry, now);
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

    private static async Task BackfillSeededProductTemplatesAsync(
        AuctionHouseDbContext dbContext,
        Dictionary<string, ProductTemplate> templateCache)
    {
        var now = DateTime.UtcNow;
        var seededProducts = await dbContext.Products
            .Include(product => product.Category)
            .Where(product =>
                product.ProductTemplateId == null &&
                product.Auctions.Any(auction =>
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
            .ToListAsync();

        if (seededProducts.Count == 0)
        {
            return;
        }

        foreach (var product in seededProducts)
        {
            if (product.Category is null)
            {
                continue;
            }

            var template = await GetOrCreateTemplateFromProductAsync(dbContext, product, product.Category, templateCache, now);
            product.ProductTemplateId = template.Id;
            product.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<ProductTemplate> GetOrCreateTemplateFromEntryAsync(
        AuctionHouseDbContext dbContext,
        SpreadsheetAuctionCatalog.Entry entry,
        Category category,
        Dictionary<string, ProductTemplate> cache,
        DateTime now)
    {
        var cacheKey = BuildTemplateCacheKey(category.Id, entry.Name);
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var template = await dbContext.ProductTemplates
            .FirstOrDefaultAsync(item =>
                item.DeletedAt == null &&
                item.CategoryId == category.Id &&
                item.Name == entry.Name);

        if (template is null)
        {
            template = new ProductTemplate
            {
                PrimaryImage = entry.PrimaryImage,
                IsActive = true,
                CreatedAt = now
            };

            dbContext.ProductTemplates.Add(template);
        }

        ApplyTemplateFieldsFromEntry(template, entry, category.Id);
        template.UpdatedAt = now;
        await dbContext.SaveChangesAsync();

        cache[cacheKey] = template;
        return template;
    }

    private static async Task<ProductTemplate> GetOrCreateTemplateFromProductAsync(
        AuctionHouseDbContext dbContext,
        Product product,
        Category category,
        Dictionary<string, ProductTemplate> cache,
        DateTime now)
    {
        var cacheKey = BuildTemplateCacheKey(category.Id, product.Name);
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var template = await dbContext.ProductTemplates
            .FirstOrDefaultAsync(item =>
                item.DeletedAt == null &&
                item.CategoryId == category.Id &&
                item.Name == product.Name);

        if (template is null)
        {
            template = new ProductTemplate
            {
                Name = product.Name,
                CategoryId = category.Id,
                SetName = product.SetName,
                CardNumber = product.CardNumber,
                GradeLabel = product.GradeLabel,
                Year = product.Year,
                Language = product.Language,
                ShortDescription = product.ShortDescription,
                DescriptionHtml = product.DescriptionHtml,
                PrimaryImage = product.PrimaryImage,
                IsActive = true,
                CreatedAt = now
            };

            dbContext.ProductTemplates.Add(template);
            await dbContext.SaveChangesAsync();
        }

        cache[cacheKey] = template;
        return template;
    }

    private static string BuildTemplateCacheKey(int categoryId, string name) =>
        $"{categoryId}:{name.Trim()}";

    private static async Task SyncBuyNowPricesAsync(AuctionHouseDbContext dbContext)
    {
        var priceMap = SpreadsheetAuctionCatalog.GetBuyNowPriceMap();
        var auctions = await dbContext.Auctions
            .Include(auction => auction.Product)
            .Where(auction =>
                auction.AuctionEventName != null &&
                LegacySeedEventNames.Contains(auction.AuctionEventName) &&
                auction.Product.Name != null)
            .ToListAsync();

        if (auctions.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var auction in auctions)
        {
            var productName = auction.Product.Name!;
            decimal? catalogPrice = null;
            if (priceMap.TryGetValue(productName, out var mappedPrice))
            {
                catalogPrice = mappedPrice;
            }

            if (auction.BuyNowPrice == catalogPrice)
            {
                continue;
            }

            auction.BuyNowPrice = catalogPrice;
            auction.UpdatedAt = DateTime.UtcNow;
            changed = true;
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
        var productIds = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.AuctionEventName != null &&
                LegacySeedEventNames.Contains(auction.AuctionEventName))
            .Select(auction => auction.ProductId)
            .Distinct()
            .ToListAsync();

        if (productIds.Count == 0)
        {
            return;
        }

        await DeleteSeededProductsAsync(dbContext, productIds);
        await DeleteOrphanedSeedTemplatesAsync(dbContext);
    }

    private static async Task DeleteSeededProductsAsync(
        AuctionHouseDbContext dbContext,
        IReadOnlyList<int> productIds)
    {
        if (productIds.Count == 0)
        {
            return;
        }

        var testAuctions = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction => productIds.Contains(auction.ProductId))
            .Select(auction => new { auction.Id, auction.ProductId })
            .ToListAsync();

        if (testAuctions.Count == 0)
        {
            return;
        }

        var auctionIds = testAuctions.Select(auction => auction.Id).ToList();
        var affectedProductIds = testAuctions.Select(auction => auction.ProductId).Distinct().ToList();

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
            .Where(image => affectedProductIds.Contains(image.ProductId))
            .ExecuteDeleteAsync();

        await dbContext.ProductDocuments
            .Where(document => affectedProductIds.Contains(document.ProductId))
            .ExecuteDeleteAsync();

        await dbContext.Products
            .Where(product => affectedProductIds.Contains(product.Id))
            .Where(product => !product.Auctions.Any())
            .ExecuteDeleteAsync();
    }

    private static async Task DeleteOrphanedSeedTemplatesAsync(AuctionHouseDbContext dbContext)
    {
        var catalogNames = SpreadsheetAuctionCatalog.GetEntries()
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphanedTemplates = await dbContext.ProductTemplates
            .Where(template =>
                template.DeletedAt == null &&
                !template.Products.Any() &&
                catalogNames.Contains(template.Name))
            .ToListAsync();

        if (orphanedTemplates.Count == 0)
        {
            return;
        }

        dbContext.ProductTemplates.RemoveRange(orphanedTemplates);
        await dbContext.SaveChangesAsync();
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
        var normalizedName = CategorySlug.NormalizeForCompare(categoryName);
        var categories = await dbContext.Categories.ToListAsync();
        var category = categories.FirstOrDefault(item =>
            item.Name == categoryName
            || item.Slug == slug
            || CategorySlug.NormalizeForCompare(item.Name) == normalizedName);

        if (category is null)
        {
            category = new Category
            {
                Name = categoryName,
                Slug = slug,
                IsActive = true,
                SortOrder = categoryName switch
                {
                    _ when categoryName.Equals("One Piece", StringComparison.OrdinalIgnoreCase) => 1,
                    _ when categoryName.Equals("Yu-Gi-Oh!", StringComparison.OrdinalIgnoreCase) => 2,
                    _ when categoryName.Equals("Pokémon", StringComparison.OrdinalIgnoreCase) => 3,
                    _ when categoryName.Equals("Sports", StringComparison.OrdinalIgnoreCase) => 4,
                    _ => 5
                },
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }
        else if (!category.IsActive)
        {
            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        cache[categoryName] = category;
        return category;
    }
}
