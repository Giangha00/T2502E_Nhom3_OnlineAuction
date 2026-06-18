using System.Data;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderCreationService : IOrderCreationService
{
    private const decimal ShippingFee = 45m;

    private readonly AuctionHouseDbContext _dbContext;
    private readonly ILogger<OrderCreationService> _logger;

    public OrderCreationService(
        AuctionHouseDbContext dbContext,
        ILogger<OrderCreationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var candidateAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.ListingType == ListingTypes.Auction &&
                (auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon) &&
                auction.DeletedAt == null)
            .Select(auction => new { auction.Id, auction.EndDate })
            .ToListAsync(cancellationToken);

        var auctionIds = candidateAuctions
            .Where(auction => !DateTimeUtilities.IsInFutureUtc(auction.EndDate))
            .Select(auction => auction.Id)
            .ToList();

        var createdCount = 0;
        foreach (var auctionId in auctionIds)
        {
            var orderId = await CreatePendingPaymentOrderForAuctionAsync(auctionId, cancellationToken);
            if (orderId.HasValue)
            {
                createdCount++;
            }
        }

        return createdCount;
    }

    public Task<int?> CreatePendingPaymentOrderForAuctionAsync(
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => CreatePendingPaymentOrderCoreAsync(auctionId, cancellationToken));
    }

    private async Task<int?> CreatePendingPaymentOrderCoreAsync(
        int auctionId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var existingOrderId = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.AuctionId == auctionId)
            .Select(item => (int?)item.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOrderId.HasValue)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOrderId;
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var winningBid = await _dbContext.Bids
            .Where(bid => bid.AuctionId == auctionId && bid.IsWinning)
            .OrderByDescending(bid => bid.Amount)
            .ThenByDescending(bid => bid.PlacedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (winningBid is null)
        {
            auction.Status = AuctionStatuses.Ended;
            auction.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        var subtotal = winningBid.Amount;
        var insurance = Math.Round(Math.Max(60m, subtotal * 0.00721m), 2);
        var total = subtotal + ShippingFee + insurance;

        var order = new AuctionOrder
        {
            OrderReference = $"AH-{now:yyyyMMdd}-{auction.Id}",
            BuyerId = winningBid.BidderId,
            Subtotal = subtotal,
            ShippingFee = ShippingFee,
            VaultInsurance = insurance,
            TotalAmount = total,
            Status = OrderStatuses.PendingPayment,
            PaymentDeadline = now.AddHours(48),
            CreatedAt = now,
            Items =
            [
                new OrderItem
                {
                    AuctionId = auction.Id,
                    ItemName = auction.Product.Name,
                    ItemGrade = auction.Product.GradeLabel,
                    ItemImageUrl = auction.Product.PrimaryImage,
                    WinningBid = subtotal,
                    CreatedAt = now
                }
            ]
        };

        _dbContext.Orders.Add(order);
        auction.Status = AuctionStatuses.AwaitingPayment;
        auction.WinnerId = winningBid.BidderId;
        auction.UpdatedAt = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Order creation raced for auction {AuctionId}.", auctionId);
            await transaction.RollbackAsync(cancellationToken);

            return await _dbContext.OrderItems
                .AsNoTracking()
                .Where(item => item.AuctionId == auctionId)
                .Select(item => (int?)item.OrderId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return order.Id;
    }
}
