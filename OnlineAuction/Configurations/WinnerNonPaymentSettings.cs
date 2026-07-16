namespace OnlineAuction.Configurations;

public class WinnerNonPaymentSettings
{
    public const string SectionName = "WinnerNonPayment";

    /// <summary>
    /// Hours the second-chance winner has to complete payment (same window as initial winner by default).
    /// </summary>
    public int SecondChancePaymentHours { get; set; } = 48;
}
