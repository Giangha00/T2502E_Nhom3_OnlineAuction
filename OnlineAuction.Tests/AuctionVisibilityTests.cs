using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class AuctionVisibilityTests
{
    [Fact]
    public async Task GetAuctionIndexAsync_IncludesApprovedLiveAuction()
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        SeedAuction(db, 1, AuctionStatuses.Live, ListingTypes.Auction, now.AddDays(-7), now.AddDays(-5), now.AddHours(-1), now.AddHours(2));
        await db.SaveChangesAsync();

        var model = await CreateService(db).GetAuctionIndexAsync();

        var item = Assert.Single(model.Auctions);
        Assert.Equal(1, item.Id);
        Assert.Equal(AuctionListingPhases.LiveAuction, item.ListingPhase);
    }

    [Fact]
    public async Task GetAuctionIndexAsync_IncludesApprovedScheduledAuctionBeforeRegistrationStart()
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        SeedAuction(db, 2, AuctionStatuses.Scheduled, ListingTypes.Auction, now.AddDays(2), now.AddDays(3), now.AddDays(5), now.AddDays(5).AddHours(1));
        await db.SaveChangesAsync();

        var model = await CreateService(db).GetAuctionIndexAsync();

        var item = Assert.Single(model.Auctions);
        Assert.Equal(2, item.Id);
        Assert.Equal(AuctionListingPhases.Upcoming, item.ListingPhase);
        Assert.Equal("registration_start", item.PhaseCountdownKind);
        Assert.True(item.IsPubliclyListed);
        Assert.Equal("Yes", item.PublicListingStatus);
        Assert.Contains("Upcoming", item.PublicListingReason);
    }

    [Theory]
    [InlineData(AuctionStatuses.PendingReview)]
    [InlineData(AuctionStatuses.Rejected)]
    [InlineData(AuctionStatuses.Cancelled)]
    public async Task GetAuctionIndexAsync_ExcludesNonPublicAuctionStatuses(string status)
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        SeedAuction(db, 3, status, ListingTypes.Auction, now.AddDays(-1), now.AddDays(1), now.AddDays(2), now.AddDays(2).AddHours(1));
        await db.SaveChangesAsync();

        var model = await CreateService(db).GetAuctionIndexAsync();

        Assert.Empty(model.Auctions);
    }

    [Fact]
    public async Task GetAuctionIndexAsync_ExcludesBuyNowListings()
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        SeedAuction(db, 4, AuctionStatuses.Live, ListingTypes.BuyNow, now.AddDays(-7), now.AddDays(-5), now.AddHours(-1), now.AddHours(2));
        await db.SaveChangesAsync();

        var auctionModel = await CreateService(db).GetAuctionIndexAsync();
        var buyNowModel = await CreateService(db).GetBuyNowIndexAsync();

        Assert.Empty(auctionModel.Auctions);
        Assert.Single(buyNowModel.Auctions);
    }

    [Fact]
    public void IsPubliclyListed_ReturnsTrueForScheduledApprovedAuctionBeforeRegistrationStart()
    {
        var now = DateTime.UtcNow;
        var auction = BuildAuction(5, AuctionStatuses.Scheduled, ListingTypes.Auction, now.AddDays(2), now.AddDays(3), now.AddDays(5), now.AddDays(5).AddHours(1));

        Assert.True(AuctionScheduleHelper.IsPubliclyListed(auction, now));
    }

    private static AuctionHouseDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuctionHouseDbContext(options);
    }

    private static AuctionService CreateService(AuctionHouseDbContext db) =>
        new(db, Options.Create(new PlatformFeeSettings()));

    private static void SeedAuction(
        AuctionHouseDbContext db,
        int id,
        string status,
        string listingType,
        DateTime registrationStart,
        DateTime registrationEnd,
        DateTime startDate,
        DateTime endDate)
    {
        var category = db.Categories.Local.FirstOrDefault()
            ?? new Category
            {
                Id = 1,
                Name = "Trading Cards",
                Slug = "trading-cards",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };

        if (db.Entry(category).State == EntityState.Detached)
        {
            db.Categories.Add(category);
        }

        var auction = BuildAuction(id, status, listingType, registrationStart, registrationEnd, startDate, endDate);
        auction.Product = new Product
        {
            Id = id,
            SellerId = 10,
            CategoryId = category.Id,
            Category = category,
            Name = $"Card {id}",
            PrimaryImage = "/img/card.png",
            GradeLabel = "PSA 10",
            CreatedAt = DateTime.UtcNow
        };
        auction.ProductId = auction.Product.Id;

        db.Products.Add(auction.Product);
        db.Auctions.Add(auction);
    }

    private static Auction BuildAuction(
        int id,
        string status,
        string listingType,
        DateTime registrationStart,
        DateTime registrationEnd,
        DateTime startDate,
        DateTime endDate) =>
        new()
        {
            Id = id,
            Status = status,
            ListingType = listingType,
            RequiresRegistration = listingType == ListingTypes.Auction,
            StartingPrice = 100m,
            CurrentPrice = 100m,
            BidStep = 5m,
            BuyNowPrice = listingType == ListingTypes.BuyNow ? 150m : null,
            RegistrationStartDate = DateTimeUtilities.AsUtc(registrationStart),
            RegistrationEndDate = DateTimeUtilities.AsUtc(registrationEnd),
            StartDate = DateTimeUtilities.AsUtc(startDate),
            EndDate = DateTimeUtilities.AsUtc(endDate),
            SubmittedAt = DateTime.UtcNow.AddDays(-1),
            VerifiedAt = status is AuctionStatuses.Scheduled or AuctionStatuses.Live or AuctionStatuses.EndingSoon
                ? DateTime.UtcNow
                : null,
            CreatedAt = DateTime.UtcNow
        };
}
