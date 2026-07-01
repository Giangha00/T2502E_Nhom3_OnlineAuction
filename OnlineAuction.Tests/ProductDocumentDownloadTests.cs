using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class ProductDocumentDownloadTests
{
    [Fact]
    public async Task GetDownloadAsync_LiveAuction_AllowsAnonymousDownload()
    {
        await using var db = await CreateContextAsync();
        var documentId = await SeedDocumentAsync(db, AuctionStatuses.Live);

        var service = new ProductDocumentDownloadService(db);
        var result = await service.GetDownloadAsync(documentId, isAdminRequest: false);

        Assert.NotNull(result);
        Assert.Equal(ProductDocumentDownloadStatus.Success, result!.Status);
        Assert.Contains("fl_attachment", result.FileUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDownloadAsync_PendingReview_DeniesAnonymousDownload()
    {
        await using var db = await CreateContextAsync();
        var documentId = await SeedDocumentAsync(db, AuctionStatuses.PendingReview);

        var service = new ProductDocumentDownloadService(db);
        var result = await service.GetDownloadAsync(documentId, isAdminRequest: false);

        Assert.NotNull(result);
        Assert.Equal(ProductDocumentDownloadStatus.Forbidden, result!.Status);
    }

    [Fact]
    public async Task GetDownloadAsync_DeletedDocument_ReturnsNotFound()
    {
        await using var db = await CreateContextAsync();
        var documentId = await SeedDocumentAsync(db, AuctionStatuses.Live, deleted: true);

        var service = new ProductDocumentDownloadService(db);
        var result = await service.GetDownloadAsync(documentId, isAdminRequest: false);

        Assert.NotNull(result);
        Assert.Equal(ProductDocumentDownloadStatus.NotFound, result!.Status);
    }

    [Fact]
    public async Task GetDownloadAsync_Admin_AllowsPendingReviewDocument()
    {
        await using var db = await CreateContextAsync();
        var documentId = await SeedDocumentAsync(db, AuctionStatuses.PendingReview);

        var service = new ProductDocumentDownloadService(db);
        var result = await service.GetDownloadAsync(documentId, isAdminRequest: true);

        Assert.NotNull(result);
        Assert.Equal(ProductDocumentDownloadStatus.Success, result!.Status);
        Assert.Equal("PSA Certificate.pdf", result.FileName);
    }

    [Theory]
    [InlineData("PSA Certificate", "PSA Certificate.pdf")]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("bad/name", "badname.pdf")]
    public void SanitizeDownloadFileName_NormalizesOutput(string input, string expected)
    {
        var actual = ProductDocumentFileHelper.SanitizeDownloadFileName(input);
        Assert.Equal(expected, actual);
    }

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

    private static async Task<int> SeedDocumentAsync(
        AuctionHouseDbContext db,
        string auctionStatus,
        bool deleted = false)
    {
        var product = new Product
        {
            SellerId = 1,
            CategoryId = 1,
            Name = "Test Card",
            PrimaryImage = "https://example.com/image.jpg",
            CreatedAt = DateTime.UtcNow
        };

        var liveStart = DateTime.UtcNow.AddDays(-1);
        var auction = new Auction
        {
            Product = product,
            Status = auctionStatus,
            StartingPrice = 100m,
            BidStep = 10m,
            CurrentPrice = 100m,
            RegistrationStartDate = liveStart.AddDays(-7),
            RegistrationEndDate = liveStart,
            StartDate = liveStart,
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        var document = new ProductDocument
        {
            Product = product,
            Name = "PSA Certificate",
            FileUrl = "https://res.cloudinary.com/demo/image/upload/v1/auction-house/documents/cert.pdf",
            FileType = "PDF",
            CreatedAt = DateTime.UtcNow,
            DeletedAt = deleted ? DateTime.UtcNow : null
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        db.ProductDocuments.Add(document);
        await db.SaveChangesAsync();

        return document.Id;
    }
}
