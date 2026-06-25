using Microsoft.EntityFrameworkCore;

namespace OnlineAuction.Data;

public static class ProductNumberBackfill
{
    public static async Task BackfillMissingAsync(AuctionHouseDbContext dbContext)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE products SET product_number = printf('PRD-%08d', id) WHERE product_number IS NULL OR product_number = '';");
            return;
        }

        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE products SET product_number = CONCAT('PRD-', LPAD(CAST(id AS CHAR), 8, '0')) WHERE product_number IS NULL OR product_number = '';");
            return;
        }

        var productsWithoutNumber = await dbContext.Products
            .Where(product => product.ProductNumber == null || product.ProductNumber == string.Empty)
            .OrderBy(product => product.Id)
            .ToListAsync();

        if (productsWithoutNumber.Count == 0)
        {
            return;
        }

        foreach (var product in productsWithoutNumber)
        {
            product.ProductNumber = $"PRD-{product.Id:D8}";
        }

        await dbContext.SaveChangesAsync();
    }
}
