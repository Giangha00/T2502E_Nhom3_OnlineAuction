using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SellerAuctionService : ISellerAuctionService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IAvatarStorageService _imageStorageService;

    public SellerAuctionService(
        AuctionHouseDbContext dbContext,
        IAvatarStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _imageStorageService = imageStorageService;
    }

    public async Task<(bool Success, string Message)> CreateAsync(CreateAuctionViewModel model, int sellerId)
    {
        var category = await ResolveCategoryAsync(model.Category, sellerId);
        if (category is null)
        {
            return (false, "Invalid category.");
        }

        var imageUrl = await _imageStorageService.SaveAvatarAsync(model.PrimaryImageFile);
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return (false, "Product image is required.");
        }

        var product = new Product
        {
            SellerId = sellerId,
            CategoryId = category.Id,
            Name = model.ProductName.Trim(),
            ShortDescription = model.Subtitle?.Trim(),
            DescriptionHtml = model.ProductDescription,
            Condition = model.Condition,
            Year = model.Year,
            SetName = model.SetName,
            GradeLabel = model.Grade,
            CertNumber = model.CertificateNumber,
            PrimaryImage = imageUrl,
            CreatedBy = sellerId
        };

        var auction = new Auction
        {
            Product = product,
            StartingPrice = model.StartingPrice,
            BidStep = model.BidStep,
            CurrentPrice = model.StartingPrice,
            BuyNowPrice = model.BuyNowPrice,
            Status = AuctionStatuses.Live,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            CreatedBy = sellerId
        };

        await _dbContext.Auctions.AddAsync(auction);
        await _dbContext.SaveChangesAsync();

        return (true, $"Your auction \"{model.ProductName}\" has been created successfully!");
    }

    private async Task<Category?> ResolveCategoryAsync(string categoryName, int sellerId)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        var normalizedName = categoryName.Trim();
        var slug = ToSlug(normalizedName);

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(item =>
                item.Name == normalizedName ||
                item.Slug == slug);

        if (category is not null)
        {
            return category;
        }

        category = new Category
        {
            Name = normalizedName,
            Slug = slug,
            SortOrder = await _dbContext.Categories.CountAsync() + 1,
            IsActive = true,
            CreatedBy = sellerId
        };

        await _dbContext.Categories.AddAsync(category);
        await _dbContext.SaveChangesAsync();

        return category;
    }

    private static string ToSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "category" : slug;
    }
}
