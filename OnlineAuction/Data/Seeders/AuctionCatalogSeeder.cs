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

    /// <summary>6 public Buy Now pages × 12 cards per page.</summary>
    private const int TargetLiveBuyNowCount = 72;
    private const int TargetProductsPerTemplate = 4;

    private sealed record BestSellerProfile(
        string Email,
        string UserName,
        string FullName,
        string PhoneNumber,
        string AvatarUrl,
        string Password,
        string[] LegacyEmails);

    private static readonly BestSellerProfile[] BestSellerProfiles =
    [
        new(
            "vietanh@yopmail.com",
            "vietanh",
            "Phạm Việt Anh",
            "0901000001",
            "/images/team/pham-viet-anh.png",
            "Vietanh00",
            ["viet.anh@auctionhouse.local"]),
        new(
            "giangha@yopmail.com",
            "giangha",
            "Nguyễn Giang Hà",
            "0901000002",
            "/images/team/nguyen-giang-ha.png",
            "Giangha00",
            ["giangha@auctionhouse.local"]),
        new(
            "dinhhai@yopmail.com",
            "dinhhai",
            "Đinh Văn Hải",
            "0901000003",
            "/images/team/dinh-van-hai.png",
            "Dinhhai00",
            ["nguyen.hai@auctionhouse.local"]),
        new(
            "nguyenhung@yopmail.com",
            "nguyenhung",
            "Nguyễn Văn Hưng",
            "0901000004",
            "/images/team/nguyen-van-hung.png",
            "Nguyenhung00",
            ["van.hung@auctionhouse.local"]),
        new(
            "huuquan@yopmail.com",
            "huuquan",
            "Nguyễn Hữu Quân",
            "0901000005",
            "/images/team/nguyen-huu-quan.png",
            "Huuquan00",
            ["huu.quan@auctionhouse.local"]),
        new(
            "danil@yopmail.com",
            "danil",
            "Danil Fomin Long",
            "0901000006",
            "/images/team/danil-fomin-long.png",
            "Danil00",
            ["dan.long@auctionhouse.local"])
    ];

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

        var sellerIds = await EnsureBestSellersAsync(userManager);
        if (sellerIds.Count == 0)
        {
            return;
        }

        var bidder = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user3@auctionhouse.local" && u.Status == UserStatus.Active);

        // When not wiping, always load existing seeded products so restarts UPDATE
        // (stable IDs) instead of INSERT duplicates / burning identity values.
        var existingProducts = refreshInDevelopment
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

            var sellerId = ResolveSellerId(sellerIds, seedIndex);
            if (existingProducts.TryGetValue(entry.Name, out var existingProduct))
            {
                await SyncSeededEntryAsync(
                    dbContext,
                    existingProduct,
                    entry,
                    sellerId,
                    categoryCache,
                    templateCache,
                    now);
                seedIndex++;
                continue;
            }

            await SeedEntryAsync(dbContext, entry, sellerId, bidder?.Id, categoryCache, templateCache, now, seedIndex);
            seedIndex++;
        }

        if (syncCatalog && !refreshInDevelopment)
        {
            await RemoveOrphanedSeededProductsAsync(dbContext, catalogNames);
        }

        await BackfillSeededProductTemplatesAsync(dbContext, templateCache);
        await SyncBuyNowPricesAsync(dbContext);
        await EnsureDedicatedBuyNowListingsAsync(
            dbContext,
            sellerIds,
            categoryCache,
            templateCache,
            now);
        await EnsureTemplateSampleInstancesAsync(dbContext, now, sellerIds);
        await EnsureFullFlowDemoScheduleAsync(dbContext, DateTime.UtcNow);
        await EnsureSharedBestSellerBiddingScenarioAsync(dbContext, DateTime.UtcNow);
        await SyncSeedCategoriesAsync(dbContext);

        if (!refreshInDevelopment)
        {
            await ReactivateExpiredSeededListingsAsync(dbContext, now);
        }
    }

    private static int ResolveSellerId(IReadOnlyList<int> sellerIds, int seedIndex) =>
        sellerIds[seedIndex % sellerIds.Count];

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

    private static async Task<IReadOnlyList<int>> EnsureBestSellersAsync(UserManager<ApplicationUser> userManager)
    {
        var sellerIds = new List<int>(BestSellerProfiles.Length);

        foreach (var profile in BestSellerProfiles)
        {
            var user = await EnsureSellerAsync(userManager, profile);
            if (user is not null)
            {
                sellerIds.Add(user.Id);
            }
        }

        return sellerIds;
    }

    private static async Task<ApplicationUser?> EnsureSellerAsync(
        UserManager<ApplicationUser> userManager,
        BestSellerProfile profile)
    {
        var avatarUrl = string.IsNullOrWhiteSpace(profile.AvatarUrl)
            ? "/admin/images/user/user-01.jpg"
            : profile.AvatarUrl;

        ApplicationUser? byEmail = await userManager.FindByEmailAsync(profile.Email);
        ApplicationUser? byUserName = await userManager.FindByNameAsync(profile.UserName);
        ApplicationUser? byLegacy = null;
        foreach (var legacyEmail in profile.LegacyEmails)
        {
            byLegacy = await userManager.FindByEmailAsync(legacyEmail);
            if (byLegacy is not null)
            {
                break;
            }
        }

        // Prefer username/legacy owner so existing catalog SellerId rows stay attached.
        var user = byUserName ?? byLegacy ?? byEmail;

        // If the target email already belongs to a different row, free it so we can
        // move the published demo email onto the canonical seller account.
        if (user is not null && byEmail is not null && byEmail.Id != user.Id)
        {
            byEmail.Email = $"migrated-{byEmail.Id}-{profile.UserName}@auctionhouse.local";
            byEmail.NormalizedEmail = userManager.NormalizeEmail(byEmail.Email);
            if (string.Equals(byEmail.UserName, profile.UserName, StringComparison.OrdinalIgnoreCase))
            {
                byEmail.UserName = $"migrated-{byEmail.Id}-{profile.UserName}";
                byEmail.NormalizedUserName = userManager.NormalizeName(byEmail.UserName);
            }

            await userManager.UpdateAsync(byEmail);
            byEmail = null;
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = profile.UserName,
                Email = profile.Email,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Role = UserRole.User,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, profile.Password);
            if (result.Succeeded)
            {
                return user;
            }

            user = await userManager.FindByEmailAsync(profile.Email)
                ?? await userManager.FindByNameAsync(profile.UserName);
            if (user is null)
            {
                return null;
            }
        }

        var needsUpdate = false;

        if (!string.Equals(user.Email, profile.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await userManager.FindByEmailAsync(profile.Email);
            if (emailOwner is null || emailOwner.Id == user.Id)
            {
                user.Email = profile.Email;
                user.NormalizedEmail = userManager.NormalizeEmail(profile.Email);
                needsUpdate = true;
            }
        }

        if (!string.Equals(user.UserName, profile.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var usernameOwner = await userManager.FindByNameAsync(profile.UserName);
            if (usernameOwner is null || usernameOwner.Id == user.Id)
            {
                user.UserName = profile.UserName;
                user.NormalizedUserName = userManager.NormalizeName(profile.UserName);
                needsUpdate = true;
            }
        }

        if (!string.Equals(user.FullName, profile.FullName, StringComparison.Ordinal))
        {
            user.FullName = profile.FullName;
            needsUpdate = true;
        }

        if (!string.Equals(user.PhoneNumber, profile.PhoneNumber, StringComparison.Ordinal))
        {
            user.PhoneNumber = profile.PhoneNumber;
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

        if (!string.Equals(user.AvatarUrl, avatarUrl, StringComparison.Ordinal))
        {
            user.AvatarUrl = avatarUrl;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await userManager.UpdateAsync(user);
        }

        // Always sync demo passwords (Remove+Add is more reliable than reset-token after email/username migration).
        await SyncSellerPasswordAsync(userManager, user, profile.Password);

        if (user.AccessFailedCount > 0 || user.LockoutEnd is not null)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }

        return user;
    }

    private static async Task SyncSellerPasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        if (await userManager.CheckPasswordAsync(user, password))
        {
            return;
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, password);
            if (reset.Succeeded)
            {
                return;
            }

            await userManager.RemovePasswordAsync(user);
        }

        await userManager.AddPasswordAsync(user, password);
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
                product.DeletedAt == null &&
                product.Auctions.Any(auction =>
                    auction.DeletedAt == null &&
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
            .ToListAsync();

        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Name))
            .GroupBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            // Prefer the oldest row so we keep a stable identity when duplicates already exist.
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(product => product.Id).First(),
                StringComparer.OrdinalIgnoreCase);
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
        product.Condition = NormalizeCondition(entry.Condition);
        product.Year = entry.Year;
        product.SetName = NormalizeTextOrNull(entry.SetName);
        product.Language = NormalizeTextOrNull(entry.Language);
        product.CardNumber = NormalizeCardNumberOrNull(entry.CardNumber);
        product.GradeLabel = entry.GradeLabel;
        product.CertNumber = product.Condition == "graded" && !string.IsNullOrWhiteSpace(product.CardNumber)
            ? $"{entry.GradeLabel.Replace(" ", "-")}-{product.CardNumber.Replace("/", "-")}"
            : null;
        product.PrimaryImage = entry.PrimaryImage;

        var startingPrice = ResolveValidStartingPrice(entry);
        product.EstimatedValue = startingPrice;
        product.ImportPrice = Math.Round(startingPrice * 0.8m, 2, MidpointRounding.AwayFromZero);
        product.ProductOrigin = NormalizeTextOrNull(entry.Language);
    }

    private static void ApplyTemplateFieldsFromEntry(
        ProductTemplate template,
        SpreadsheetAuctionCatalog.Entry entry,
        int categoryId)
    {
        template.Name = entry.Name;
        template.CategoryId = categoryId;
        template.SetName = NormalizeTextOrNull(entry.SetName);
        template.CardNumber = NormalizeCardNumberOrNull(entry.CardNumber);
        template.GradeLabel = entry.GradeLabel;
        template.Year = entry.Year;
        template.Language = NormalizeTextOrNull(entry.Language);
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
        var startingPrice = ResolveValidStartingPrice(entry);
        var bidStep = SpreadsheetAuctionCatalog.ComputeBidStep(startingPrice);

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
            StartingPrice = startingPrice,
            BidStep = bidStep,
            CurrentPrice = startingPrice,
            BuyNowPrice = null,
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
        var amount = startingPrice;

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

    private static decimal ResolveValidStartingPrice(SpreadsheetAuctionCatalog.Entry entry) =>
        entry.StartingPrice > 0 ? entry.StartingPrice : 1m;

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

    private static string NormalizeCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return "graded";
        }

        var value = condition.Trim();
        if (value.Contains('|') || value.Length > 40)
        {
            return "graded";
        }

        return value;
    }

    private static string? NormalizeTextOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("N a", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalized;
    }

    private static string? NormalizeCardNumberOrNull(string? value) => NormalizeTextOrNull(value);

    private static async Task SyncBuyNowPricesAsync(AuctionHouseDbContext dbContext)
    {
        var priceMap = SpreadsheetAuctionCatalog.GetBuyNowPriceMap();
        // Only auction listings get optional catalog Buy Now prices.
        // Dedicated Buy Now rows use ListingType=buynow and must keep BuyNowPrice set.
        var auctions = await dbContext.Auctions
            .Include(auction => auction.Product)
            .Where(auction =>
                auction.ListingType == ListingTypes.Auction &&
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

    private const string DedicatedBuyNowEventName = "RareCard Vault Buy Now";

    private static string BuildDedicatedBuyNowProductName(string catalogName, int sequence) =>
        sequence <= 1
            ? $"{catalogName} [Buy Now]"
            : $"{catalogName} [Buy Now] #{sequence}";

    private static bool TryGetCatalogNameFromDedicatedBuyNowProduct(string? productName, out string catalogName)
    {
        catalogName = string.Empty;
        if (string.IsNullOrWhiteSpace(productName))
        {
            return false;
        }

        const string marker = " [Buy Now]";
        var markerIndex = productName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            return false;
        }

        catalogName = productName[..markerIndex].Trim();
        return catalogName.Length > 0;
    }

    private static decimal ResolveDedicatedBuyNowStartingPrice(decimal buyNowPrice) =>
        buyNowPrice <= 0.01m ? 0.01m : buyNowPrice - 0.01m;

    private static decimal ResolveBuyNowPrice(SpreadsheetAuctionCatalog.Entry entry)
    {
        if (SpreadsheetAuctionCatalog.TryGetBuyNowPrice(entry.Name) is { } mapped)
        {
            return mapped;
        }

        // Fallback so CHECK(buy_now_price > starting_price) still holds after ResolveDedicatedBuyNowStartingPrice.
        return Math.Max(entry.StartingPrice + 25m, 50m);
    }

    private static decimal ResolveBuyNowPriceForDedicatedListing(
        Auction auction,
        IReadOnlyDictionary<string, SpreadsheetAuctionCatalog.Entry> entriesByName)
    {
        if (TryGetCatalogNameFromDedicatedBuyNowProduct(auction.Product?.Name, out var catalogName))
        {
            if (SpreadsheetAuctionCatalog.TryGetBuyNowPrice(catalogName) is { } mapped)
            {
                return mapped;
            }

            if (entriesByName.TryGetValue(catalogName, out var entry))
            {
                return ResolveBuyNowPrice(entry);
            }
        }

        if (auction.CurrentPrice > auction.StartingPrice)
        {
            return auction.CurrentPrice;
        }

        return Math.Max(auction.StartingPrice + 25m, 50m);
    }

    private static void ApplyDedicatedBuyNowPricing(
        Auction auction,
        IReadOnlyDictionary<string, SpreadsheetAuctionCatalog.Entry> entriesByName)
    {
        var buyNowPrice = ResolveBuyNowPriceForDedicatedListing(auction, entriesByName);
        var startingPrice = ResolveDedicatedBuyNowStartingPrice(buyNowPrice);

        if (auction.BuyNowPrice == buyNowPrice
            && auction.StartingPrice == startingPrice
            && auction.CurrentPrice == buyNowPrice
            && auction.ListingType == ListingTypes.BuyNow)
        {
            return;
        }

        auction.ListingType = ListingTypes.BuyNow;
        auction.BuyNowPrice = buyNowPrice;
        auction.StartingPrice = startingPrice;
        auction.CurrentPrice = buyNowPrice;
        auction.BidStep = 0.01m;
        auction.RequiresRegistration = false;
        auction.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyDedicatedBuyNowSchedule(Auction auction, DateTime now)
    {
        auction.Status = AuctionStatuses.Live;
        auction.EndDate = now.AddYears(1);
        auction.StartDate = now.AddMinutes(-5);
        auction.RegistrationStartDate = auction.StartDate.AddMinutes(-1);
        auction.RegistrationEndDate = auction.StartDate;
        auction.VerifiedAt ??= now;
        auction.WinnerId = null;
        auction.UpdatedAt = now;
    }

    private static async Task EnsureDedicatedBuyNowListingsAsync(
        AuctionHouseDbContext dbContext,
        IReadOnlyList<int> sellerIds,
        Dictionary<string, Category> categoryCache,
        Dictionary<string, ProductTemplate> templateCache,
        DateTime now)
    {
        if (sellerIds.Count == 0)
        {
            return;
        }

        var entriesByName = SpreadsheetAuctionCatalog.GetEntries()
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var dedicated = await dbContext.Auctions
            .Include(auction => auction.Product)
            .Where(auction =>
                auction.DeletedAt == null &&
                auction.Product.DeletedAt == null &&
                auction.ListingType == ListingTypes.BuyNow &&
                auction.AuctionEventName == DedicatedBuyNowEventName)
            .OrderBy(auction => auction.Id)
            .ToListAsync();

        for (var i = 0; i < dedicated.Count; i++)
        {
            var auction = dedicated[i];
            auction.Product.SellerId = ResolveSellerId(sellerIds, i);
            auction.Product.UpdatedAt = now;

            ApplyDedicatedBuyNowPricing(auction, entriesByName);

            if (auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon)
                || auction.EndDate <= now)
            {
                ApplyDedicatedBuyNowSchedule(auction, now);
            }
        }

        if (dedicated.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        var existingNames = dedicated
            .Select(auction => auction.Product.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var liveCount = dedicated.Count(auction =>
            auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon
            && auction.EndDate > now
            && auction.BuyNowPrice != null
            && auction.BuyNowPrice > auction.StartingPrice);

        if (liveCount >= TargetLiveBuyNowCount)
        {
            return;
        }

        var catalogEntries = SpreadsheetAuctionCatalog.GetEntries();
        if (catalogEntries.Count == 0)
        {
            return;
        }

        var createdIndex = dedicated.Count;
        var safety = 0;
        while (liveCount < TargetLiveBuyNowCount && safety < TargetLiveBuyNowCount * 3)
        {
            safety++;
            var entry = catalogEntries[createdIndex % catalogEntries.Count];
            var sequence = (createdIndex / catalogEntries.Count) + 1;
            var productName = BuildDedicatedBuyNowProductName(entry.Name, sequence);
            if (!existingNames.Add(productName))
            {
                createdIndex++;
                continue;
            }

            var sellerId = ResolveSellerId(sellerIds, createdIndex);
            var buyNowPrice = ResolveBuyNowPrice(entry);
            var startingPrice = ResolveDedicatedBuyNowStartingPrice(buyNowPrice);
            var category = await GetOrCreateCategoryAsync(dbContext, entry.CategoryName, categoryCache);
            var template = await GetOrCreateTemplateFromEntryAsync(dbContext, entry, category, templateCache, now);
            var liveStart = now.AddMinutes(-5);

            var product = new Product
            {
                Category = category,
                CreatedAt = now
            };

            ApplyProductFieldsFromEntry(product, entry, sellerId, category.Id, template.Id);
            product.Name = productName;
            product.ShortDescription = TruncatePlainText(entry.Description, 300);
            product.DescriptionHtml = entry.Description;

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            SyncProductImages(dbContext, product, entry, now);
            await dbContext.SaveChangesAsync();

            var auction = new Auction
            {
                ProductId = product.Id,
                StartingPrice = startingPrice,
                BidStep = 0.01m,
                CurrentPrice = buyNowPrice,
                BuyNowPrice = buyNowPrice,
                ListingType = ListingTypes.BuyNow,
                RequiresRegistration = false,
                AuctionEventName = DedicatedBuyNowEventName,
                RegistrationStartDate = liveStart.AddMinutes(-1),
                RegistrationEndDate = liveStart,
                StartDate = liveStart,
                EndDate = now.AddYears(1),
                Status = AuctionStatuses.Live,
                SubmittedAt = null,
                VerifiedAt = now,
                CreatedAt = now
            };

            dbContext.Auctions.Add(auction);
            await dbContext.SaveChangesAsync();

            createdIndex++;
            liveCount++;
        }
    }

    private static string TruncatePlainText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var plain = value.Trim();
        return plain.Length <= maxLength ? plain : plain[..maxLength];
    }

    private static async Task EnsureTemplateSampleInstancesAsync(
        AuctionHouseDbContext dbContext,
        DateTime now,
        IReadOnlyList<int> sellerIds)
    {
        var templates = await dbContext.ProductTemplates
            .AsNoTracking()
            .Where(template => template.DeletedAt == null && template.IsActive)
            .Where(template => dbContext.Products.Any(product =>
                product.ProductTemplateId == template.Id &&
                product.DeletedAt == null &&
                product.Auctions.Any(auction =>
                    auction.DeletedAt == null &&
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName))))
            .Select(template => new { template.Id })
            .ToListAsync();

        if (templates.Count == 0)
        {
            return;
        }

        var seedIndex = 0;
        foreach (var template in templates)
        {
            var products = await dbContext.Products
                .Include(product => product.Images)
                .Include(product => product.Auctions)
                .Where(product => product.ProductTemplateId == template.Id && product.DeletedAt == null)
                .Where(product => product.Auctions.Any(auction =>
                    auction.DeletedAt == null &&
                    auction.AuctionEventName != null &&
                    LegacySeedEventNames.Contains(auction.AuctionEventName)))
                .OrderBy(product => product.Id)
                .ToListAsync();

            if (products.Count == 0)
            {
                continue;
            }

            var missing = TargetProductsPerTemplate - products.Count;
            if (missing <= 0)
            {
                continue;
            }

            var baseProduct = products[0];
            var baseAuction = baseProduct.Auctions
                .Where(auction => auction.DeletedAt == null && auction.AuctionEventName == SpreadsheetAuctionCatalog.TestAuctionEventName)
                .OrderBy(auction => auction.Id)
                .FirstOrDefault();

            if (baseAuction is null)
            {
                continue;
            }

            for (var i = 0; i < missing; i++)
            {
                var sellerId = sellerIds.Count == 0
                    ? baseProduct.SellerId
                    : ResolveSellerId(sellerIds, seedIndex++);

                var clone = new Product
                {
                    SellerId = sellerId,
                    CategoryId = baseProduct.CategoryId,
                    ProductTemplateId = baseProduct.ProductTemplateId,
                    Name = baseProduct.Name,
                    ShortDescription = baseProduct.ShortDescription,
                    Subtitle = baseProduct.Subtitle,
                    DescriptionHtml = baseProduct.DescriptionHtml,
                    Condition = baseProduct.Condition,
                    ProductOrigin = baseProduct.ProductOrigin,
                    Year = baseProduct.Year,
                    SetName = baseProduct.SetName,
                    Language = baseProduct.Language,
                    CardNumber = baseProduct.CardNumber,
                    GradeLabel = baseProduct.GradeLabel,
                    CertNumber = baseProduct.CertNumber,
                    GradingCentering = baseProduct.GradingCentering,
                    GradingCorners = baseProduct.GradingCorners,
                    GradingEdges = baseProduct.GradingEdges,
                    GradingSurface = baseProduct.GradingSurface,
                    PrimaryImage = baseProduct.PrimaryImage,
                    EstimatedValue = baseProduct.EstimatedValue,
                    ImportPrice = baseProduct.ImportPrice,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.Products.Add(clone);
                await dbContext.SaveChangesAsync();

                var sortOrder = 0;
                foreach (var image in baseProduct.Images
                    .Where(image => image.DeletedAt == null)
                    .OrderBy(image => image.SortOrder))
                {
                    dbContext.ProductImages.Add(new ProductImage
                    {
                        ProductId = clone.Id,
                        ImageUrl = image.ImageUrl,
                        SortOrder = sortOrder++,
                        CreatedAt = now
                    });
                }

                var cloneAuction = new Auction
                {
                    ProductId = clone.Id,
                    StartingPrice = baseAuction.StartingPrice,
                    BidStep = baseAuction.BidStep,
                    CurrentPrice = baseAuction.StartingPrice,
                    BuyNowPrice = null,
                    ListingType = ListingTypes.Auction,
                    RequiresRegistration = true,
                    AuctionEventName = SpreadsheetAuctionCatalog.TestAuctionEventName,
                    Status = AuctionStatuses.Live,
                    CreatedAt = now
                };

                AuctionScheduleHelper.ApplyTestAuctionSchedule(cloneAuction, clone.Id, now);
                dbContext.Auctions.Add(cloneAuction);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private static async Task EnsureSharedBestSellerBiddingScenarioAsync(
        AuctionHouseDbContext dbContext,
        DateTime now)
    {
        var bidderNames = new[]
        {
            "Nguyễn Giang Hà",
            "Đinh Văn Hải",
            "Nguyễn Văn Hưng"
        };

        var bidders = await dbContext.Users
            .Where(user => bidderNames.Contains(user.FullName) && user.Status == UserStatus.Active)
            .OrderBy(user => user.Id)
            .ToListAsync();

        if (bidders.Count < 3)
        {
            return;
        }

        var auction = await dbContext.Auctions
            .Include(item => item.Product)
            .Include(item => item.Bids)
            .Include(item => item.Registrations)
            .FirstOrDefaultAsync(item =>
                item.DeletedAt == null &&
                item.Product.DeletedAt == null &&
                item.ListingType == ListingTypes.Auction &&
                item.AuctionEventName == SpreadsheetAuctionCatalog.TestAuctionEventName &&
                item.Product.Name == SpreadsheetAuctionCatalog.FullFlowDemoProductName);

        if (auction is null)
        {
            return;
        }

        if (auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon))
        {
            auction.Status = AuctionStatuses.Live;
        }

        if (auction.StartDate >= now)
        {
            auction.StartDate = now.AddMinutes(-30);
        }

        if (auction.EndDate <= now)
        {
            auction.EndDate = now.AddHours(2);
        }

        auction.RegistrationStartDate = auction.StartDate.AddDays(-1);
        auction.RegistrationEndDate = auction.StartDate.AddMinutes(-5);
        auction.RequiresRegistration = true;
        auction.UpdatedAt = now;

        foreach (var bidder in bidders)
        {
            var registration = auction.Registrations
                .FirstOrDefault(item => item.UserId == bidder.Id && item.DeletedAt == null);

            if (registration is null)
            {
                registration = new AuctionRegistration
                {
                    AuctionId = auction.Id,
                    UserId = bidder.Id,
                    Status = AuctionRegistrationStatuses.Approved,
                    RegisteredAt = now.AddMinutes(-25),
                    ReviewedAt = now.AddMinutes(-24),
                    CreatedAt = now.AddMinutes(-25),
                    UpdatedAt = now
                };
                dbContext.AuctionRegistrations.Add(registration);
            }
            else if (registration.Status != AuctionRegistrationStatuses.Approved)
            {
                registration.Status = AuctionRegistrationStatuses.Approved;
                registration.ReviewedAt = now.AddMinutes(-24);
                registration.UpdatedAt = now;
            }
        }

        var activeBids = auction.Bids
            .Where(item => item.DeletedAt == null)
            .OrderBy(item => item.PlacedAt)
            .ThenBy(item => item.Id)
            .ToList();

        foreach (var bidder in bidders)
        {
            if (activeBids.Any(item => item.BidderId == bidder.Id))
            {
                continue;
            }

            var nextAmount = (activeBids.LastOrDefault()?.Amount ?? auction.CurrentPrice) + auction.BidStep;
            if (nextAmount <= auction.CurrentPrice)
            {
                nextAmount = auction.CurrentPrice + Math.Max(auction.BidStep, 1m);
            }

            var bid = new Bid
            {
                AuctionId = auction.Id,
                BidderId = bidder.Id,
                Amount = nextAmount,
                BidType = BidTypes.Manual,
                IsWinning = false,
                PlacedAt = now.AddMinutes(-10 + activeBids.Count),
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Bids.Add(bid);
            activeBids.Add(bid);
        }

        if (activeBids.Count > 0)
        {
            foreach (var bid in activeBids)
            {
                bid.IsWinning = false;
            }

            var winningBid = activeBids
                .OrderByDescending(item => item.Amount)
                .ThenByDescending(item => item.PlacedAt)
                .First();
            winningBid.IsWinning = true;

            auction.CurrentPrice = winningBid.Amount;
            auction.WinnerId = winningBid.BidderId;
        }

        await dbContext.SaveChangesAsync();
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

        var entriesByName = SpreadsheetAuctionCatalog.GetEntries()
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var changed = false;

        foreach (var auction in seededListings)
        {
            var isDedicatedBuyNow =
                auction.ListingType == ListingTypes.BuyNow
                || string.Equals(auction.AuctionEventName, DedicatedBuyNowEventName, StringComparison.Ordinal);

            // Dedicated Buy Now catalog must stay purchasable with a fixed price.
            if (isDedicatedBuyNow)
            {
                var needsRepair = IsExpiredSeededListing(auction, now)
                    || auction.BuyNowPrice is null
                    || auction.BuyNowPrice <= auction.StartingPrice
                    || auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon);

                if (!needsRepair)
                {
                    continue;
                }

                ApplyDedicatedBuyNowPricing(auction, entriesByName);
                ApplyDedicatedBuyNowSchedule(auction, now);
                changed = true;
                continue;
            }

            if (!IsExpiredSeededListing(auction, now))
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
            await dbContext.Complaints
                .Where(complaint => complaint.OrderId != null && orderIds.Contains(complaint.OrderId.Value))
                .ExecuteDeleteAsync();

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
