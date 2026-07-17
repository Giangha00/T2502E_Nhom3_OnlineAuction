namespace OnlineAuction.Configurations;

/// <summary>
/// Development-only helpers for the release smoke pack (Signup → Login → Register+Deposit → Bid).
/// Must stay disabled outside local/demo environments.
/// </summary>
public sealed class SmokeTestingSettings
{
    public const string SectionName = "SmokeTesting";

    public bool Enabled { get; set; }
}
