using OnlineAuction.Entities;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class OrderCheckoutSelectionTests
{
    private static readonly DateTime Now = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_IncludesAllAuctionWinsAndSelectedBuyNow()
    {
        var orders = new List<AuctionOrder>
        {
            CreateOrder(1, OrderSources.AuctionWin, 100),
            CreateOrder(2, OrderSources.AuctionWin, 200),
            CreateOrder(3, OrderSources.BuyNow, 300),
            CreateOrder(4, OrderSources.BuyNow, 400)
        };

        var result = OrderCheckoutSelection.Resolve(orders, [3], Now);

        Assert.True(result.Success);
        Assert.Equal(3, result.Orders.Count);
        Assert.Contains(result.Orders, order => order.Id == 1);
        Assert.Contains(result.Orders, order => order.Id == 2);
        Assert.Contains(result.Orders, order => order.Id == 3);
        Assert.DoesNotContain(result.Orders, order => order.Id == 4);
    }

    [Fact]
    public void Resolve_OnlyBuyNowWithoutSelection_ReturnsError()
    {
        var orders = new List<AuctionOrder>
        {
            CreateOrder(3, OrderSources.BuyNow, 300),
            CreateOrder(4, OrderSources.BuyNow, 400)
        };

        var result = OrderCheckoutSelection.Resolve(orders, [], Now);

        Assert.False(result.Success);
        Assert.Equal(OrderCheckoutSelection.ErrorNoSelection, result.Message);
    }

    [Fact]
    public void Resolve_OnlyAuctionWins_DoesNotRequireExplicitSelection()
    {
        var orders = new List<AuctionOrder>
        {
            CreateOrder(1, OrderSources.AuctionWin, 100)
        };

        var result = OrderCheckoutSelection.Resolve(orders, [], Now);

        Assert.True(result.Success);
        Assert.Single(result.Orders);
    }

    [Fact]
    public void Resolve_ExcludesExpiredOrders()
    {
        var orders = new List<AuctionOrder>
        {
            CreateOrder(1, OrderSources.AuctionWin, 100, deadlineHours: -1)
        };

        var result = OrderCheckoutSelection.Resolve(orders, [], Now);

        Assert.False(result.Success);
        Assert.Equal(OrderCheckoutSelection.ErrorNoPending, result.Message);
    }

    [Fact]
    public void ResolveOrderSource_FallsBackToReferencePrefix()
    {
        var buyNowOrder = new AuctionOrder
        {
            OrderReference = "BN-20260617-10",
            OrderSource = string.Empty
        };

        Assert.Equal(OrderSources.BuyNow, OrderCheckoutSelection.ResolveOrderSource(buyNowOrder));
    }

    private static AuctionOrder CreateOrder(
        int id,
        string source,
        decimal subtotal,
        int deadlineHours = 24)
    {
        return new AuctionOrder
        {
            Id = id,
            OrderSource = source,
            OrderReference = source == OrderSources.BuyNow
                ? $"BN-20260617-{id}"
                : $"AH-20260617-{id}",
            Subtotal = subtotal,
            TotalAmount = subtotal + 45m + 60m,
            PaymentDeadline = Now.AddHours(deadlineHours),
            Status = OrderStatuses.PendingPayment
        };
    }
}
