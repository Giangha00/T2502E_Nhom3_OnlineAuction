namespace OnlineAuction.Messaging.Messages;

public enum EmailSendKind
{
    PasswordResetOtp = 1,
    EmailConfirmation = 2
}

public sealed class EmailSendMessage
{
    public EmailSendKind Kind { get; init; }

    public string To { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Locale { get; init; } = "en-US";

    public string? OtpCode { get; init; }

    public int? ExpiryMinutes { get; init; }

    public string? ConfirmUrl { get; init; }
}
