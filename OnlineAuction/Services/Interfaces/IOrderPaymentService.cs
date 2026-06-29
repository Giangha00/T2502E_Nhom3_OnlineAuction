using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;

namespace OnlineAuction.Services.Interfaces;

public interface IOrderPaymentService
{
    Task<PayPalCheckoutResult> InitiatePayPalCheckoutAsync(
        int buyerId,
        IReadOnlyList<int> selectedOrderIds,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<PayPalCaptureCheckoutResult> CapturePayPalCheckoutAsync(
        int buyerId,
        string payPalOrderId,
        CancellationToken cancellationToken = default);

    Task CancelPayPalCheckoutAsync(
        int buyerId,
        string? payPalOrderId,
        CancellationToken cancellationToken = default);

    Task<PaymentConfirmationViewModel?> GetPaidOrderConfirmationAsync(
        int buyerId,
        int orderId,
        CancellationToken cancellationToken = default);
    Task<string> TestProcessIpnAsync(
        string payPalOrderId,
        string transactionId,
        string paymentStatus,
        CancellationToken cancellationToken = default);
}
