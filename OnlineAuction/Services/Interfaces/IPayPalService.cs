using Microsoft.AspNetCore.Http;
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

    Task<PayPalOrderDetailsResult> GetOrderDetailsAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default);

    Task<PayPalCaptureResult> CaptureOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default);

    Task<PayPalCancelResult> CancelOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default);

    Task<PayPalVerifyWebhookResult> VerifyWebhookSignatureAsync(
        string requestBody,
        IHeaderDictionary headers,
        CancellationToken cancellationToken = default);

    // Hoàn tiền cho một capture đã thanh toán thành công.
    // captureId: PayPal capture id đã lưu trong deposit.PayPalCaptureId.
    // amount: số tiền muốn refund, thường là deposit.Amount.
    // Kết quả trả về refund id để mình lưu vào database.
    Task<PayPalRefundResult> RefundCaptureAsync(
        string captureId,
        decimal amount,
        CancellationToken cancellationToken = default);
}
