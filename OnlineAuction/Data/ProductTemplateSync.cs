using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;

namespace OnlineAuction.Data;

public static class ProductTemplateSync
{
    public static async Task SyncAsync(AuctionHouseDbContext dbContext)
    {
        var products = await dbContext.Products
            .Where(product => product.DeletedAt == null)
            .OrderBy(product => product.Id)
            .ToListAsync();

        if (products.Count == 0)
        {
            return;
        }

        var auctionPrices = await LoadLatestAuctionPricesAsync(
            dbContext,
            products.Select(product => product.Id).ToList());

        var templates = await dbContext.ProductTemplates
            .Where(template => template.DeletedAt == null)
            .ToListAsync();

        var templatesByKey = BuildTemplateLookup(templates, products);
        var usedSlugs = templates
            .Select(template => template.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;

        foreach (var group in products.GroupBy(ProductGroupingKey.GetKey))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                continue;
            }

            if (!templatesByKey.TryGetValue(group.Key, out var template))
            {
                var seedProduct = group.OrderBy(product => product.Id).First();
                var templateName = ProductGroupingKey.BuildTemplateName(seedProduct);
                template = new ProductTemplate
                {
                    Name = templateName,
                    ShortDescription = seedProduct.ShortDescription,
                    DescriptionHtml = seedProduct.DescriptionHtml,
                    PrimaryImage = seedProduct.PrimaryImage,
                    CategoryId = seedProduct.CategoryId,
                    Slug = await CreateUniqueSlugAsync(templateName, usedSlugs),
                    CreatedAt = now
                };

                dbContext.ProductTemplates.Add(template);
                await dbContext.SaveChangesAsync();
                templatesByKey[group.Key] = template;
            }

            foreach (var product in group)
            {
                product.ProductTemplateId = template.Id;
                ApplyPricing(product, auctionPrices);
            }
        }

        await dbContext.SaveChangesAsync();
        await EnsureProductPricingAsync(dbContext);
        await RemoveEmptyTemplatesAsync(dbContext, now);
    }

    public static async Task<ProductTemplate> ResolveTemplateForProductAsync(
        AuctionHouseDbContext dbContext,
        Product product,
        int? createdBy)
    {
        var groupingKey = ProductGroupingKey.GetKey(product);
        var templates = await dbContext.ProductTemplates
            .Where(template => template.DeletedAt == null)
            .ToListAsync();

        var products = await dbContext.Products
            .Where(item => item.DeletedAt == null && item.ProductTemplateId != null)
            .Select(item => new { item.Id, item.ProductTemplateId, item.CategoryId, item.SetName, item.CardNumber, item.Name })
            .ToListAsync();

        var templateIdByKey = products
            .GroupBy(item => ProductGroupingKey.GetKey(new Product
            {
                CategoryId = item.CategoryId,
                SetName = item.SetName,
                CardNumber = item.CardNumber,
                Name = item.Name
            }))
            .Where(group => group.First().ProductTemplateId.HasValue)
            .ToDictionary(
                group => group.Key,
                group => group.First().ProductTemplateId!.Value);

        if (templateIdByKey.TryGetValue(groupingKey, out var existingTemplateId))
        {
            var linked = templates.FirstOrDefault(template => template.Id == existingTemplateId);
            if (linked is not null)
            {
                return linked;
            }
        }

        var existingByName = templates.FirstOrDefault(
            template => CategorySlug.NormalizeForCompare(template.Name)
                == CategorySlug.NormalizeForCompare(ProductGroupingKey.BuildTemplateName(product)));

        if (existingByName is not null)
        {
            return existingByName;
        }

        var usedSlugs = templates
            .Select(template => template.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var templateName = ProductGroupingKey.BuildTemplateName(product);
        var template = new ProductTemplate
        {
            Name = templateName,
            ShortDescription = product.ShortDescription,
            DescriptionHtml = product.DescriptionHtml,
            PrimaryImage = product.PrimaryImage,
            CategoryId = product.CategoryId,
            Slug = await CreateUniqueSlugAsync(templateName, usedSlugs),
            CreatedAt = now,
            CreatedBy = createdBy
        };

        dbContext.ProductTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        return template;
    }

    private static Dictionary<string, ProductTemplate> BuildTemplateLookup(
        IReadOnlyCollection<ProductTemplate> templates,
        IReadOnlyCollection<Product> products)
    {
        var lookup = new Dictionary<string, ProductTemplate>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products.Where(product => product.ProductTemplateId.HasValue))
        {
            var key = ProductGroupingKey.GetKey(product);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var template = templates.FirstOrDefault(item => item.Id == product.ProductTemplateId);
            if (template is not null)
            {
                lookup[key] = template;
            }
        }

        foreach (var template in templates)
        {
            var key = CategorySlug.NormalizeForCompare(template.Name);
            if (!string.IsNullOrWhiteSpace(key))
            {
                lookup.TryAdd(key, template);
            }
        }

        return lookup;
    }

    private static async Task RemoveEmptyTemplatesAsync(AuctionHouseDbContext dbContext, DateTime now)
    {
        var emptyTemplateIds = await dbContext.ProductTemplates
            .Where(template => template.DeletedAt == null)
            .Where(template => !template.Products.Any(product => product.DeletedAt == null))
            .Select(template => template.Id)
            .ToListAsync();

        if (emptyTemplateIds.Count == 0)
        {
            return;
        }

        var emptyTemplates = await dbContext.ProductTemplates
            .Where(template => emptyTemplateIds.Contains(template.Id))
            .ToListAsync();

        foreach (var template in emptyTemplates)
        {
            template.DeletedAt = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureProductPricingAsync(AuctionHouseDbContext dbContext)
    {
        var productsMissingPrice = await dbContext.Products
            .Where(product => product.DeletedAt == null && product.Price == null)
            .Select(product => product.Id)
            .ToListAsync();

        if (productsMissingPrice.Count == 0)
        {
            return;
        }

        var auctionPrices = await LoadLatestAuctionPricesAsync(dbContext, productsMissingPrice);

        var products = await dbContext.Products
            .Where(product => productsMissingPrice.Contains(product.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            ApplyPricing(product, auctionPrices);
        }

        await dbContext.SaveChangesAsync();
    }

    private static void ApplyPricing(Product product, IReadOnlyDictionary<int, decimal> auctionPrices)
    {
        if (product.Price.HasValue && product.Price.Value > 0)
        {
            return;
        }

        if (product.EstimatedValue is > 0)
        {
            product.Price = product.EstimatedValue;
            return;
        }

        if (product.ImportPrice is > 0)
        {
            product.Price = product.ImportPrice;
            return;
        }

        if (auctionPrices.TryGetValue(product.Id, out var auctionPrice) && auctionPrice > 0)
        {
            product.Price = auctionPrice;
        }

        if (product.Quantity <= 0)
        {
            product.Quantity = 1;
        }
    }

    private static Task<string> CreateUniqueSlugAsync(string name, ISet<string> usedSlugs)
    {
        var baseSlug = CategorySlug.ToSlug(name);
        var slug = baseSlug;
        var suffix = 1;

        while (!usedSlugs.Add(slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return Task.FromResult(slug);
    }

    private static async Task<Dictionary<int, decimal>> LoadLatestAuctionPricesAsync(
        AuctionHouseDbContext dbContext,
        IReadOnlyCollection<int> productIds)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var auctions = await dbContext.Auctions
            .AsNoTracking()
            .Where(auction => productIds.Contains(auction.ProductId) && auction.DeletedAt == null)
            .Select(auction => new
            {
                auction.ProductId,
                auction.CreatedAt,
                auction.CurrentPrice,
                auction.StartingPrice
            })
            .ToListAsync();

        return auctions
            .GroupBy(auction => auction.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(auction => auction.CreatedAt)
                    .Select(auction => auction.CurrentPrice > 0 ? auction.CurrentPrice : auction.StartingPrice)
                    .FirstOrDefault());
    }
}
