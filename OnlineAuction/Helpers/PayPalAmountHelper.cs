namespace OnlineAuction.Helpers;

public static class PayPalAmountHelper
{
    public const decimal AmountTolerance = 0.01m;

    public static bool AmountsMatch(decimal expected, decimal actual) =>
        Math.Abs(expected - actual) < AmountTolerance;
}
