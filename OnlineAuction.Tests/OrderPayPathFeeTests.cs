using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class OrderPayPathFeeTests
{
    private static readonly PlatformFeeSettings FeeSettings = new()
    {
        BuyerCheckoutFeePercent = 2.50m,
        SellerSuccessFeePercent = 10.00m,
        RegistrationDepositPercent = 10.00m,
        MinimumRegistrationDeposit = 1.00m
    };

    [Fact]
    public async Task CodComplete_SetsSellerSettlementAndCreatesSuccessPayment()
    {
        await using var db = CreateDb();
        var buyer = new ApplicationUser
        {
            Id = 1,
            UserName = "buyer",
            NormalizedUserName = "BUYER",
            Email = "buyer@test.com",
            NormalizedEmail = "BUYER@TEST.COM",
            FullName = "Buyer",
            PhoneNumber = "000",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(buyer);

        var order = new AuctionOrder
        {
            Id = 10,
            OrderReference = "ORD-COD-10",
            BuyerId = 1,
            Subtotal = 100.00m,
            ShippingFee = 45.00m,
            PlatformFee = 2.50m,
            TotalAmount = 147.50m,
            Status = OrderStatuses.PendingPayment,
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var service = new OrderService(db, Options.Create(FeeSettings), new NoOpWinnerNonPaymentRecoveryService());
        var (success, _) = await service.CompleteOrderAsync(1, new Models.CompleteOrderRequest
        {
            PaymentMethod = "cod",
            SelectedOrderIds = [],
            FullName = "Buyer",
            Address = "1 Main St",
            City = "City",
            Phone = "000"
        });

        Assert.True(success);

        var paid = await db.Orders.SingleAsync(o => o.Id == 10);
        Assert.Equal(OrderStatuses.Paid, paid.Status);
        Assert.Equal(10.00m, paid.SellerFee);
        Assert.Equal(90.00m, paid.SellerProceeds);
        Assert.Equal("cod", paid.PaymentMethod);

        var payment = await db.Payments.SingleAsync(p => p.OrderId == 10);
        Assert.Equal(PaymentStatuses.Success, payment.Status);
        Assert.Equal(147.50m, payment.Amount);
        Assert.Equal("COD-ORD-COD-10", payment.TransactionId);
        Assert.NotNull(payment.PaidAt);
    }

    private static AuctionHouseDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuctionHouseDbContext(options);
    }

    private sealed class NoOpWinnerNonPaymentRecoveryService : IWinnerNonPaymentRecoveryService
    {
        public Task ProcessExpiredAuctionWinOrderAsync(
            AuctionOrder cancelledOrder,
            DateTime now,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
