namespace OnlineAuction.Configurations;

public class EmailVerificationSettings
{
    public const string SectionName = "EmailVerification";

    /// <summary>
    /// Development-only: skip Gmail and auto-confirm new accounts after signup.
    /// </summary>
    public bool UseMockEmailConfirmation { get; set; }
}
