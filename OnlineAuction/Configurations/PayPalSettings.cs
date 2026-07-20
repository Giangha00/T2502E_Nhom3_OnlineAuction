namespace OnlineAuction.Configurations;

public class PayPalSettings
{
    public const string SectionName = "PayPal";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>sandbox or live</summary>
    public string Mode { get; set; } = "sandbox";

    public string ReturnUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;

    public string WebhookId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Starting balance for each user's simulated PayPal sandbox wallet.
    /// Only used when <see cref="IsSandbox"/> is true. PayPal does not expose real buyer balances.
    /// </summary>
    public decimal SandboxInitialWalletBalance { get; set; } = 1000m;

    public bool IsConfigured =>
        HasValidCredential(ClientId) && HasValidCredential(ClientSecret);

    private static bool HasValidCredential(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("REPLACE", StringComparison.OrdinalIgnoreCase);

    public bool IsSandbox =>
        !Mode.Equals("live", StringComparison.OrdinalIgnoreCase);

    public string ApiBaseUrl =>
        IsSandbox
            ? "https://api-m.sandbox.paypal.com"
            : "https://api-m.paypal.com";
}
