using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Auctions;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class AdminAuctionFormSyncTests
{
    [Fact]
    public async Task ADM_SYNC_01_CreateAuction_PersistsFullProductSpecsAndGallery()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var service = CreateService(db);
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new AuctionFormViewModel
        {
            ProductName = "Charizard Holo",
            CategoryId = 1,
            SellerId = 1,
            ShortDescription = "Mint vintage card",
            ProductDescription = "<p>Detailed description</p>",
            Subtitle = "Base Set chase",
            Year = 1999,
            Authenticator = "PSA",
            GradeValue = "10",
            SetName = "Base Set",
            Language = "English",
            CardNumber = "4/102",
            CertificateNumber = "12345678",
            StartingPrice = 500m,
            BidStep = 25m,
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.Auction,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg"),
            GalleryImageFiles = [CreateFormFile("gallery.jpg", "image/jpeg")]
        };
        model.NormalizeGrading();

        var result = await service.CreateAsync(model);

        Assert.True(result.Success, result.Message);

        var product = await db.Products
            .Include(p => p.Images)
            .SingleAsync();

        Assert.Equal("Charizard Holo", product.Name);
        Assert.Equal("Mint vintage card", product.ShortDescription);
        Assert.Equal("<p>Detailed description</p>", product.DescriptionHtml);
        Assert.Equal("Base Set chase", product.Subtitle);
        Assert.Equal(1999, product.Year);
        Assert.Equal("Base Set", product.SetName);
        Assert.Equal("English", product.Language);
        Assert.Equal("4/102", product.CardNumber);
        Assert.Equal("PSA 10", product.GradeLabel);
        Assert.Equal("Graded", product.Condition);
        Assert.Equal("12345678", product.CertNumber);
        Assert.NotNull(product.PrimaryImage);
        Assert.Single(product.Images, image => image.DeletedAt == null);

        var auction = await db.Auctions.SingleAsync();
        Assert.Equal(ListingTypes.Auction, auction.ListingType);
        Assert.True(auction.RequiresRegistration);
        Assert.Equal(500m, auction.StartingPrice);
        Assert.Equal(25m, auction.BidStep);
        Assert.Null(auction.BuyNowPrice);
    }

    [Fact]
    public async Task ADM_SYNC_01b_CreateAuction_OptionalBuyNowPrice_SatisfiesDbConstraint()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var service = CreateService(db);
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new AuctionFormViewModel
        {
            ProductName = "Optional Buy Now Card",
            CategoryId = 1,
            SellerId = 1,
            Year = 2020,
            Authenticator = "PSA",
            GradeValue = "10",
            StartingPrice = 500m,
            BidStep = 25m,
            BuyNowPrice = 600m,
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.Auction,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg")
        };
        model.NormalizeGrading();

        var result = await service.CreateAsync(model);

        Assert.True(result.Success, result.Message);

        var auction = await db.Auctions.SingleAsync();
        Assert.Equal(500m, auction.StartingPrice);
        Assert.Equal(600m, auction.BuyNowPrice);
    }

    [Fact]
    public async Task ADM_SYNC_02_CreateBuyNow_UsesBuyNowRulesWithoutRegistration()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var service = CreateService(db);
        var model = new AuctionFormViewModel
        {
            ProductName = "Pikachu Promo",
            CategoryId = 1,
            SellerId = 1,
            Year = 2020,
            Authenticator = "PSA",
            GradeValue = "9",
            Price = 250m,
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.BuyNow,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg")
        };
        model.NormalizeGrading();

        var result = await service.CreateAsync(model);

        Assert.True(result.Success, result.Message);

        var auction = await db.Auctions.SingleAsync();
        Assert.Equal(ListingTypes.BuyNow, auction.ListingType);
        Assert.False(auction.RequiresRegistration);
        Assert.Equal(250m, auction.CurrentPrice);
        Assert.Equal(250m, auction.BuyNowPrice);
        Assert.Equal(249.99m, auction.StartingPrice);
        Assert.Equal(0.01m, auction.BidStep);
    }

    [Fact]
    public async Task ADM_SYNC_03_PublicMapper_ShowsGradeYearSubtitle()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var service = CreateService(db);
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new AuctionFormViewModel
        {
            ProductName = "Mewtwo EX",
            CategoryId = 1,
            SellerId = 1,
            Subtitle = "Legendary Collection",
            Year = 2001,
            Authenticator = "BGS",
            GradeValue = "9.5",
            StartingPrice = 300m,
            BidStep = 10m,
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            Status = AuctionStatuses.Live,
            ListingType = ListingTypes.Auction,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg")
        };
        model.NormalizeGrading();

        var createResult = await service.CreateAsync(model);
        Assert.True(createResult.Success, createResult.Message);

        var product = await db.Products.SingleAsync();

        Assert.Equal("BGS 9.5", product.GradeLabel);
        Assert.Equal("Legendary Collection", product.Subtitle);
        Assert.Equal(2001, product.Year);
    }

    [Fact]
    public void ADM_SYNC_06_ScheduleValidation_MatchesClientHelper()
    {
        var now = DateTime.UtcNow;
        var regStart = now.AddHours(1);
        var regEnd = now.AddDays(1);
        var liveStart = now.AddDays(1).AddHours(-1);
        var liveEnd = now.AddDays(2);

        var error = AuctionScheduleHelper.ValidateSchedule(regStart, regEnd, liveStart, liveEnd);

        Assert.NotNull(error);
        Assert.Contains("Registration must end before or when the live auction starts", error);
    }

    [Fact]
    public async Task ADM_SYNC_07_EditListing_LoadsAndUpdatesMissingSpecs()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var auctionId = await SeedMinimalAuctionAsync(db);
        var service = CreateService(db);

        var editForm = await service.GetEditFormAsync(auctionId);
        Assert.NotNull(editForm);
        Assert.Equal("Minimal Card", editForm!.ProductName);
        Assert.Null(editForm.Year);

        editForm.Year = 1998;
        editForm.Authenticator = "PSA";
        editForm.GradeValue = "8";
        editForm.Subtitle = "Added subtitle";
        editForm.SetName = "Jungle";
        editForm.NormalizeGrading();

        var updateResult = await service.UpdateAsync(editForm);
        Assert.True(updateResult.Success, updateResult.Message);

        var product = await db.Products.SingleAsync();
        Assert.Equal(1998, product.Year);
        Assert.Equal("PSA 8", product.GradeLabel);
        Assert.Equal("Added subtitle", product.Subtitle);
        Assert.Equal("Jungle", product.SetName);
    }

    [Fact]
    public async Task ADM_SYNC_08_UploadDocument_PersistsProductDocuments()
    {
        await using var db = await CreateContextAsync();
        await SeedReferencesAsync(db);

        var service = CreateService(db);
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new AuctionFormViewModel
        {
            ProductName = "Documented Card",
            CategoryId = 1,
            SellerId = 1,
            Year = 2022,
            Authenticator = "PSA",
            GradeValue = "10",
            StartingPrice = 100m,
            BidStep = 5m,
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.Auction,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg"),
            DocumentFiles = [CreateFormFile("cert.pdf", "application/pdf")],
            DocumentNames = ["PSA Certificate"]
        };
        model.NormalizeGrading();

        var result = await service.CreateAsync(model);
        Assert.True(result.Success, result.Message);

        var document = await db.ProductDocuments.SingleAsync();
        Assert.Equal("PSA Certificate", document.Name);
        Assert.Equal("PDF", document.FileType);
        Assert.Contains("cert.pdf", document.FileUrl);
    }

    [Fact]
    public void AuctionFormViewModel_BuyNow_SkipsAuctionScheduleValidation()
    {
        var model = new AuctionFormViewModel
        {
            ListingType = ListingTypes.BuyNow,
            ProductName = "Buy Now Card",
            CategoryId = 1,
            SellerId = 1,
            Year = 2024,
            Authenticator = "PSA",
            GradeValue = "10",
            Price = 99m,
            PrimaryImageFile = CreateFormFile("primary.jpg", "image/jpeg"),
            RegistrationStartDate = DateTime.Now.AddDays(-10),
            RegistrationEndDate = DateTime.Now.AddDays(-9),
            StartDate = DateTime.Now.AddDays(-8),
            EndDate = DateTime.Now.AddDays(-7)
        };
        model.NormalizeGrading();

        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        Assert.DoesNotContain(results, result =>
            result.ErrorMessage != null &&
            result.ErrorMessage.Contains("Registration", StringComparison.OrdinalIgnoreCase));
    }

    private static AdminAuctionService CreateService(AuctionHouseDbContext db) =>
        new(db, new FakePhotoService(), NullLogger<AdminAuctionService>.Instance);

    private static async Task<AuctionHouseDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            await pragma.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AuctionHouseDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task SeedReferencesAsync(AuctionHouseDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == 1))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = 1,
                UserName = "seller1",
                Email = "seller1@test.local",
                FullName = "Seller One",
                PhoneNumber = "0900000001",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Categories.AnyAsync(c => c.Id == 1))
        {
            db.Categories.Add(new Category
            {
                Id = 1,
                Name = "Pokemon",
                Slug = "pokemon",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedMinimalAuctionAsync(AuctionHouseDbContext db)
    {
        await SeedReferencesAsync(db);

        var now = DateTime.UtcNow;
        var product = new Product
        {
            SellerId = 1,
            CategoryId = 1,
            Name = "Minimal Card",
            PrimaryImage = "https://example.com/image.jpg",
            CreatedAt = now
        };

        var auction = new Auction
        {
            Product = product,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = true,
            StartingPrice = 50m,
            BidStep = 5m,
            CurrentPrice = 50m,
            RegistrationStartDate = now.AddDays(1),
            RegistrationEndDate = now.AddDays(7),
            StartDate = now.AddDays(7),
            EndDate = now.AddDays(7).AddHours(1),
            Status = AuctionStatuses.Confirming,
            CreatedAt = now
        };

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction.Id;
    }

    private static FormFile CreateFormFile(string fileName, string contentType)
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FakePhotoService : IPhotoService
    {
        public Task<string?> AddPhotoAsync(IFormFile? file, string folder)
        {
            if (file is null || file.Length == 0)
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>($"https://example.test/{folder}/{file.FileName}");
        }
    }
}
