namespace OnlineAuction.Services.Results;

public enum PasswordResetOtpSendStatus
{
    Sent,
    Cooldown,
    RateLimited,
    Failed
}

public enum PasswordResetOtpVerifyStatus
{
    Valid,
    Invalid,
    Expired,
    MaxAttemptsReached
}

public sealed record PasswordResetOtpSendResult(
    PasswordResetOtpSendStatus Status,
    string MaskedEmail,
    string? DevelopmentOtp = null,
    int? RetryAfterSeconds = null);

public sealed record PasswordResetOtpVerifyResult(
    PasswordResetOtpVerifyStatus Status,
    int? UserId = null,
    int? OtpId = null,
    string? MaskedEmail = null);
