namespace OnlineAuction.Configurations;

public class PasswordResetOtpSettings
{
    public const string SectionName = "PasswordResetOtp";

    public int CodeLength { get; set; } = 6;

    public int ExpiryMinutes { get; set; } = 10;

    public int MaxAttempts { get; set; } = 5;

    public int ResendCooldownSeconds { get; set; } = 60;

    public int MaxResendsPerHour { get; set; } = 3;

    public int VerifiedSessionMinutes { get; set; } = 15;

    public bool UseMockOtpSender { get; set; }
}
