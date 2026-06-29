using OnlineAuction.Services.Results;

namespace OnlineAuction.Services.Interfaces;

public interface IPasswordResetOtpService
{
    Task<PasswordResetOtpSendResult> GenerateAndSendAsync(
        string email,
        string locale,
        CancellationToken cancellationToken = default);

    Task<PasswordResetOtpVerifyResult> VerifyAsync(
        string email,
        string otpCode,
        CancellationToken cancellationToken = default);

    Task<bool> IsVerifiedOtpStillUsableAsync(
        int userId,
        int otpId,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(
        int userId,
        int otpId,
        CancellationToken cancellationToken = default);
}
