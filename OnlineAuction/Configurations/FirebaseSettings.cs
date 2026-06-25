namespace OnlineAuction.Configurations;

public class FirebaseSettings
{
    public const string SectionName = "FirebaseSettings";

    public string ProjectId { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;

    public string WebApiKey { get; set; } = string.Empty;

    public string MessagingSenderId { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public string VapidKey { get; set; } = string.Empty;

    public bool IsAdminConfigured =>
        HasValidCredential(ProjectId)
        && HasValidCredential(ClientEmail)
        && HasValidCredential(PrivateKey);

    public bool IsClientConfigured =>
        HasValidCredential(WebApiKey)
        && HasValidCredential(MessagingSenderId)
        && HasValidCredential(AppId)
        && HasValidCredential(VapidKey);

    public bool IsConfigured => IsAdminConfigured && IsClientConfigured;

    private static bool HasValidCredential(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("REPLACE", StringComparison.OrdinalIgnoreCase);
}
