using OnlineAuction.Models.PayPal;

namespace OnlineAuction.Services.Interfaces;

public interface IPayPalService
{
    Task<PayPalCreateOrderResult> CreateCheckoutOrderAsync(
        decimal totalAmount,
        string referenceId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<PayPalCaptureResult> CaptureOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default);
}
