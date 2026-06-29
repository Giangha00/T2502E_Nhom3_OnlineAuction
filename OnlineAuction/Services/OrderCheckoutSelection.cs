using OnlineAuction.Entities;

namespace OnlineAuction.Services;

public static class OrderCheckoutSelection
{
    public const string ErrorNoSelection = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
    public const string ErrorExpired = "Một hoặc nhiều hóa đơn đấu giá đã hết hạn thanh toán và không thể thanh toán.";
    public const string ErrorNoPending = "Không tìm thấy hóa đơn chờ thanh toán.";

    public static (bool Success, string Message, List<AuctionOrder> Orders) Resolve(
        IReadOnlyList<AuctionOrder> pendingOrders,
        IReadOnlyList<int>? selectedOrderIds,
        DateTime now)
    {
        var valid = pendingOrders
            .Where(order => order.PaymentDeadline > now)
            .ToList();

        if (valid.Count == 0)
        {
            return (false, ErrorNoPending, []);
        }

        var auctionWins = valid
            .Where(order => ResolveOrderSource(order) == OrderSources.AuctionWin)
            .ToList();

        var buyNowOrders = valid
            .Where(order => ResolveOrderSource(order) == OrderSources.BuyNow)
            .ToList();

        var selectedSet = selectedOrderIds?.ToHashSet() ?? [];

        var checkout = new List<AuctionOrder>(auctionWins);
        checkout.AddRange(buyNowOrders.Where(order => selectedSet.Contains(order.Id)));

        if (checkout.Count == 0)
        {
            if (buyNowOrders.Count > 0 && auctionWins.Count == 0)
            {
                return (false, ErrorNoSelection, []);
            }

            return (false, ErrorNoPending, []);
        }

        if (auctionWins.Any(order => order.PaymentDeadline <= now))
        {
            return (false, ErrorExpired, []);
        }

        return (true, string.Empty, checkout);
    }

    public static string ResolveOrderSource(AuctionOrder order)
    {
        if (!string.IsNullOrWhiteSpace(order.OrderSource))
        {
            return order.OrderSource;
        }

        if (order.OrderReference.StartsWith("BN-", StringComparison.OrdinalIgnoreCase))
        {
            return OrderSources.BuyNow;
        }

        return OrderSources.AuctionWin;
    }
}
