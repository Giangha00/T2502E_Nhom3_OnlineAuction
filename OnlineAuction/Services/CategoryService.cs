using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Categories;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class CategoryService : ICategoryService
{
    private readonly AuctionHouseDbContext _dbContext;

    public CategoryService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryListViewModel> GetCategoriesAsync(CategoryFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(category =>
                category.Name.Contains(keyword) ||
                category.Slug.Contains(keyword));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(category => category.IsActive == filter.IsActive.Value);
        }

        var dateRange = OnlineAuction.Helpers.AdminDateRangeHelper.Parse(filter.DateRange);
        if (dateRange.StartDate.HasValue && dateRange.EndDateExclusive.HasValue)
        {
            query = query.Where(category =>
                category.CreatedAt >= dateRange.StartDate.Value &&
                category.CreatedAt < dateRange.EndDateExclusive.Value);
        }
        else
        {
            if (filter.FromDate.HasValue)
            {
                query = query.Where(category => category.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var toDateExclusive = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(category => category.CreatedAt < toDateExclusive);
            }
        }

        query = filter.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(category => category.Name),
            "sort_asc" => query.OrderBy(category => category.SortOrder).ThenBy(category => category.Name),
            "sort_desc" => query.OrderByDescending(category => category.SortOrder).ThenBy(category => category.Name),
            "date_asc" => query.OrderBy(category => category.CreatedAt),
            "date_desc" => query.OrderByDescending(category => category.CreatedAt),
            _ => query.OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
        };

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var categories = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(category => new CategoryListItemViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive,
                ProductCount = category.Products.Count(product => product.DeletedAt == null),
                CreatedAt = category.CreatedAt
            })
            .ToListAsync();

        return new CategoryListViewModel
        {
            Categories = categories,
            Filter = filter,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public CategoryFormViewModel BuildCreateForm() =>
        new()
        {
            IsActive = true,
            SortOrder = 0
        };

    public async Task<CategoryFormViewModel?> GetEditFormAsync(int id)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(item => item.DeletedAt == null)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Slug,
                item.SortOrder,
                item.IsActive,
                ProductCount = item.Products.Count(product => product.DeletedAt == null)
            })
            .FirstOrDefaultAsync(item => item.Id == id);

        if (category is null)
        {
            return null;
        }

        return new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ProductCount = category.ProductCount
        };
    }

    public async Task<CategoryDetailViewModel?> GetDetailsAsync(int id)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.DeletedAt == null && category.Id == id)
            .Select(category => new CategoryDetailViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive,
                ProductCount = category.Products.Count(product => product.DeletedAt == null),
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(CategoryFormViewModel model)
    {
        var name = model.Name.Trim();
        var slug = ResolveSlug(model.Slug, name);

        var duplicateError = await ValidateUniquenessAsync(name, slug);
        if (duplicateError is not null)
        {
            return (false, duplicateError);
        }

        var category = new Category
        {
            Name = name,
            Slug = slug,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        return (true, "Category created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(CategoryFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return (false, "Category id is required.");
        }

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == model.Id.Value && item.DeletedAt == null);

        if (category is null)
        {
            return (false, "Category not found.");
        }

        var name = model.Name.Trim();
        var slug = ResolveSlug(model.Slug, name);

        var duplicateError = await ValidateUniquenessAsync(name, slug, category.Id);
        if (duplicateError is not null)
        {
            return (false, duplicateError);
        }

        category.Name = name;
        category.Slug = slug;
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return (true, "Category updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var category = await _dbContext.Categories
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (category is null)
        {
            return (false, "Category not found.");
        }

        var activeProductCount = category.Products.Count(product => product.DeletedAt == null);
        if (activeProductCount > 0)
        {
            return (false, $"Cannot delete this category because it is used by {activeProductCount} product(s). Deactivate it instead.");
        }

        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return (true, "Category deleted successfully.");
    }

    public async Task<(bool Success, string Message)> BulkDeleteAsync(IReadOnlyList<int> categoryIds)
    {
        if (categoryIds.Count == 0)
        {
            return (false, "Please select at least one category.");
        }

        var categories = await _dbContext.Categories
            .Include(item => item.Products)
            .Where(item => categoryIds.Contains(item.Id) && item.DeletedAt == null)
            .ToListAsync();

        if (categories.Count == 0)
        {
            return (false, "No categories found.");
        }

        var deletedCount = 0;
        var skippedMessages = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var category in categories)
        {
            var activeProductCount = category.Products.Count(product => product.DeletedAt == null);
            if (activeProductCount > 0)
            {
                skippedMessages.Add($"#{category.Id} {category.Name}: used by {activeProductCount} product(s)");
                continue;
            }

            category.DeletedAt = now;
            category.UpdatedAt = now;
            deletedCount++;
        }

        if (deletedCount == 0)
        {
            return (false, string.Join(" ", skippedMessages));
        }

        await _dbContext.SaveChangesAsync();

        if (skippedMessages.Count == 0)
        {
            return (true, $"Deleted {deletedCount} category(ies) successfully.");
        }

        return (true, $"Deleted {deletedCount} category(ies). Skipped {skippedMessages.Count}: {string.Join(" ", skippedMessages)}");
    }

    private static void NormalizeFilter(CategoryFilterViewModel filter)
    {
        if (filter.Page <= 0)
        {
            filter.Page = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }
    }

    private async Task<string?> ValidateUniquenessAsync(string name, string slug, int? excludeId = null)
    {
        var nameExists = await _dbContext.Categories.AnyAsync(category =>
            category.DeletedAt == null &&
            category.Name == name &&
            (!excludeId.HasValue || category.Id != excludeId.Value));

        if (nameExists)
        {
            return "A category with this name already exists.";
        }

        var slugExists = await _dbContext.Categories.AnyAsync(category =>
            category.DeletedAt == null &&
            category.Slug == slug &&
            (!excludeId.HasValue || category.Id != excludeId.Value));

        if (slugExists)
        {
            return "A category with this slug already exists.";
        }

        return null;
    }

    private static string ResolveSlug(string? slug, string name)
    {
        if (!string.IsNullOrWhiteSpace(slug))
        {
            return slug.Trim().ToLowerInvariant();
        }

        return BuildSlug(name);
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
