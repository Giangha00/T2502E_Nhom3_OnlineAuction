namespace OnlineAuction.Configurations;

public sealed class BidFraudDetectionSettings
{
    public const string SectionName = "BidFraudDetection";

    public bool Enabled { get; set; } = true;

    public bool RateLimitingEnabled { get; set; } = true;

    public int MaxBidsPerMinutePerUser { get; set; } = 10;

    public int MaxBidsPerMinutePerAuction { get; set; } = 30;

    public int SameIpAccountThreshold { get; set; } = 2;

    public int RapidBidWindowSeconds { get; set; } = 60;

    public int RapidBidCountThreshold { get; set; } = 5;

    public int CollusionRoundTripThreshold { get; set; } = 3;

    public decimal AbnormalJumpPercent { get; set; } = 50;

    public int NewAccountHoursThreshold { get; set; } = 24;

    public int AntiSnipeThresholdMinutes { get; set; } = 5;

    public int AntiSnipeExtensionMinutes { get; set; } = 5;
}
