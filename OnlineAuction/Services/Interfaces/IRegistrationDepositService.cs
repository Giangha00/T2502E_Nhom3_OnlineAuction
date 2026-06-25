using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IRegistrationDepositService
{
    // Tính tiền cọc 10%
    decimal CalculateDepositAmount(decimal? estimatedValue, decimal startingPrice);

    // User bấm đăng ký đấu giá
    // Tạo registration pending + deposit pending + PayPal order
    Task<RegistrationDepositResult> InitiateDepositAsync(
        int auctionId,
        int userId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    // PayPal return về sau khi user thanh toán
    // Capture tiền và approve registration
    Task<RegistrationDepositResult> CaptureDepositAsync(
        int userId,
        string payPalOrderId,
        CancellationToken cancellationToken = default);

    // User hủy thanh toán ở PayPal
    Task<RegistrationDepositResult> CancelDepositAsync(
        int userId,
        string payPalOrderId,
        CancellationToken cancellationToken = default);
}