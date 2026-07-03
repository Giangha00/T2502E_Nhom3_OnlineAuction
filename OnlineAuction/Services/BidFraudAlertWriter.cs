using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;

namespace OnlineAuction.Services;

public interface IBidFraudAlertWriter
{
    Task<bool> CreateAlertAsync(
        int auctionId,
        long? bidId,
        int? userId,
        string alertType,
        string severity,
        string message,
        string? metadataJson,
        string? bidFlagReason,
        CancellationToken cancellationToken = default);
}

public sealed class BidFraudAlertWriter : IBidFraudAlertWriter
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(15);

    private readonly AuctionHouseDbContext _dbContext;
    private readonly ILogger<BidFraudAlertWriter> _logger;

    public BidFraudAlertWriter(
        AuctionHouseDbContext dbContext,
        ILogger<BidFraudAlertWriter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> CreateAlertAsync(
        int auctionId,
        long? bidId,
        int? userId,
        string alertType,
        string severity,
        string message,
        string? metadataJson,
        string? bidFlagReason,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(DedupWindow);
        var duplicateExists = await _dbContext.BidFraudAlerts.AnyAsync(alert =>
            alert.AuctionId == auctionId
            && alert.AlertType == alertType
            && alert.UserId == userId
            && alert.CreatedAt >= cutoff,
            cancellationToken);

        if (duplicateExists)
        {
            return false;
        }

        if (bidId.HasValue && !string.IsNullOrWhiteSpace(bidFlagReason))
        {
            var bid = await _dbContext.Bids.FirstOrDefaultAsync(item => item.Id == bidId.Value, cancellationToken);
            if (bid is not null)
            {
                bid.IsFlagged = true;
                bid.FlagReason = Truncate(bidFlagReason, 255);
            }
        }

        _dbContext.BidFraudAlerts.Add(new BidFraudAlert
        {
            AuctionId = auctionId,
            BidId = bidId,
            UserId = userId,
            AlertType = alertType,
            Severity = severity,
            Message = message,
            MetadataJson = metadataJson,
            Status = FraudAlertStatuses.Open,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Fraud alert created: {AlertType} for auction {AuctionId} severity {Severity}.",
            alertType,
            auctionId,
            severity);

        return true;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
