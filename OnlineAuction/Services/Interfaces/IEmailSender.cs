namespace OnlineAuction.Services.Interfaces;

public interface IEmailSender
{
    Task<bool> SendPasswordResetOtpAsync(
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale,
        CancellationToken cancellationToken = default);
}
