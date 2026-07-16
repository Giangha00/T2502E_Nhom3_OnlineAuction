namespace OnlineAuction.Configurations;

public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Local testing only. Logs email content instead of sending.
    /// </summary>
    public bool UseMockEmailSender { get; set; }

    public GmailOAuthSettings Gmail { get; set; } = new();

    public SmtpSettings Smtp { get; set; } = new();
}

public class GmailOAuthSettings
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? RefreshToken { get; set; }

    public string? SenderEmail { get; set; }

    public string? SenderName { get; set; } = "RareCard Auction House";
}

public class SmtpSettings
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? SenderEmail { get; set; }

    public string? SenderName { get; set; } = "RareCard Auction House";
}
