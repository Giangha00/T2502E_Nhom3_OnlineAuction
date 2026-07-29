using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IOrderService
{
    Task<OrderPageViewModel?> BuildOrderPageAsync(int buyerId);

    Task<int> CountPendingPaymentOrdersAsync(int buyerId);

    Task<int> CancelExpiredPendingOrdersAsync(int buyerId);

    Task<int> CancelAllExpiredPendingOrdersAsync();

    Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        CompleteOrderRequest request);

    Task<(bool Success, string Message, int ClearedCount)> ClearAllBuyNowOrdersAsync(
        int buyerId,
        CancellationToken cancellationToken = default);
}
