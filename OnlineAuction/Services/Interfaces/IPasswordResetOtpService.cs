namespace OnlineAuction.Services.Interfaces;

public interface IPasswordResetOtpService
{
    string CreateOtp(string email, string resetToken);

    bool VerifyOtp(string email, string otp);

    bool TryConsumeVerifiedToken(string email, out string? resetToken);

    void Clear(string email);
}
