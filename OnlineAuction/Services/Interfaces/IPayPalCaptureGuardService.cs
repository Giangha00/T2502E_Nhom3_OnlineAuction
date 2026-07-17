using OnlineAuction.Models.PayPal;

namespace OnlineAuction.Services.Interfaces;

public interface IPayPalCaptureGuardService
{
    Task<SafePayPalCaptureResult> SafeCaptureAsync(
        string payPalOrderId,
        decimal expectedAmount,
        PayPalCaptureContext context,
        CancellationToken cancellationToken = default);
}
