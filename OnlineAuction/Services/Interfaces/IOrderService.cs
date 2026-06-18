using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IOrderService
{
    Task<OrderPageViewModel?> BuildOrderPageAsync(int buyerId);

    Task<int> CountPendingPaymentOrdersAsync(int buyerId);

    Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        string shippingAddress,
        string paymentMethod);
}
