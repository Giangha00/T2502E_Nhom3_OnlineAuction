namespace OnlineAuction.Configurations;

public sealed class BidFraudDetectionSettings
{
    public const string SectionName = "BidFraudDetection";

    public bool Enabled { get; set; } = true;

    public bool RateLimitingEnabled { get; set; } = true;

    /// <summary>
    /// Hard limit: bids per minute per user per auction. Exceeding returns HTTP 429 (no bid insert).
    /// </summary>
    public int MaxBidsPerMinutePerUser { get; set; } = 30;

    /// <summary>
    /// Hard limit: total bids per minute on an auction (all users).
    /// </summary>
    public int MaxBidsPerMinutePerAuction { get; set; } = 120;

    /// <summary>
    /// Hard limit: bids per minute per IP per auction.
    /// </summary>
    public int MaxBidsPerMinutePerIp { get; set; } = 60;

    public int SameIpAccountThreshold { get; set; } = 4;

    public int RapidBidWindowSeconds { get; set; } = 20;

    public int RapidBidCountThreshold { get; set; } = 12;

    public int CollusionRoundTripThreshold { get; set; } = 3;

    public decimal AbnormalJumpPercent { get; set; } = 50;

    public int NewAccountHoursThreshold { get; set; } = 24;

    public int AntiSnipeThresholdMinutes { get; set; } = 5;

    public int AntiSnipeExtensionMinutes { get; set; } = 5;

    public int MaxAntiSnipeExtensions { get; set; } = 3;

    public int MaxEndDateExtensionTotalMinutes { get; set; } = 15;

    /// <summary>
    /// Action when a high-severity fraud rule fires: Alert, Reject, or ShadowBan.
    /// </summary>
    public string HighSeverityAction { get; set; } = HighSeverityBidActions.Alert;

    /// <summary>
    /// Temporary shadow-ban duration after a high-severity hit (when action is ShadowBan).
    /// </summary>
    public int ShadowBanDurationMinutes { get; set; } = 30;

    public bool ChallengeEnabled { get; set; } = true;

    /// <summary>
    /// Provider name: Stub (admin-configurable accepted tokens) or None.
    /// </summary>
    public string ChallengeProvider { get; set; } = BidChallengeProviders.Stub;

    /// <summary>
    /// Soft threshold: after this many bids/minute (user+auction), a challenge token is required.
    /// Must be less than or equal to <see cref="MaxBidsPerMinutePerUser"/> to be useful.
    /// </summary>
    public int ChallengeAfterBidsPerMinute { get; set; } = 25;

    /// <summary>
    /// When true, a fraud alert marks the user as requiring a challenge on the next bid.
    /// </summary>
    public bool ChallengeAfterFraudAlert { get; set; } = true;

    /// <summary>
    /// Tokens accepted by the Stub challenge provider (admin-configurable).
    /// </summary>
    public string[] StubChallengeAcceptedTokens { get; set; } = ["stub-ok"];

    /// <summary>
    /// How long a "challenge required" flag stays in cache after an alert.
    /// </summary>
    public int ChallengeRequiredMinutes { get; set; } = 15;
}

public static class HighSeverityBidActions
{
    public const string Alert = "Alert";
    public const string Reject = "Reject";
    public const string ShadowBan = "ShadowBan";
}

public static class BidChallengeProviders
{
    public const string None = "None";
    public const string Stub = "Stub";
}
