using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class ListingFeeService : IListingFeeService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly PlatformFeeSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ListingFeeService> _logger;

    public ListingFeeService(
        AuctionHouseDbContext dbContext,
        IOptions<PlatformFeeSettings> settings,
        IHostEnvironment environment,
        ILogger<ListingFeeService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public decimal CalculateListingFee(decimal startingPrice) =>
        ListingFeeCalculator.CalculateListingFee(_settings, startingPrice);

    public string BuildPreviewDescription(decimal startingPrice) =>
        ListingFeeCalculator.BuildPreviewDescription(_settings, startingPrice);

    public async Task<bool> HasPaidListingFeeAsync(int auctionId, CancellationToken cancellationToken = default) =>
        await _dbContext.ListingFees.AsNoTracking()
            .AnyAsync(
                fee => fee.AuctionId == auctionId
                       && fee.Status == ListingFeeStatuses.Paid
                       && fee.DeletedAt == null,
                cancellationToken);

    public async Task<ListingFeeCollectionResult> CollectListingFeeAsync(
        Auction auction,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var existingFee = await _dbContext.ListingFees
            .AsNoTracking()
            .Where(fee => fee.AuctionId == auction.Id
                          && fee.Status == ListingFeeStatuses.Paid
                          && fee.DeletedAt == null)
            .Select(fee => fee.FeeAmount)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingFee > 0)
        {
            return ListingFeeCollectionResult.Succeeded(existingFee, alreadyCollected: true);
        }

        if (!CanUseMockPayment())
        {
            return ListingFeeCollectionResult.Failed(
                "Listing fee payment is not configured. Enable PlatformFee:UseMockListingFeePayment for development or configure PayPal.");
        }

        var feeAmount = CalculateListingFee(auction.StartingPrice);
        var feeType = ListingFeeCalculator.NormalizeFeeType(_settings.ListingFeeType);
        var now = DateTime.UtcNow;

        _dbContext.ListingFees.Add(new ListingFee
        {
            AuctionId = auction.Id,
            SellerId = auction.Product.SellerId,
            FeeAmount = feeAmount,
            FeeType = feeType,
            Status = ListingFeeStatuses.Paid,
            PaidAt = now,
            CreatedAt = now,
            CreatedBy = adminUserId
        });

        _logger.LogInformation(
            "Listing fee collected (mock): AuctionId={AuctionId}, SellerId={SellerId}, Amount={Amount}, AdminId={AdminId}",
            auction.Id,
            auction.Product.SellerId,
            feeAmount,
            adminUserId);

        return ListingFeeCollectionResult.Succeeded(feeAmount);
    }

    private bool CanUseMockPayment() =>
        _settings.UseMockListingFeePayment || _environment.IsDevelopment();
}
