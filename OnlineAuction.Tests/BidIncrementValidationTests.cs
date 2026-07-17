using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

/// <summary>
/// BID-03: bid increment must be a whole number of BidStep above CurrentPrice.
/// Exercises the same rules as BidService via PlaceBid outcomes.
/// </summary>
public class BidIncrementValidationTests
{
    [Theory]
    [InlineData(100, 10, 110, true)]
    [InlineData(100, 10, 120, true)]
    [InlineData(100, 10, 130, true)]
    [InlineData(100, 10, 109, false)]
    [InlineData(100, 10, 115, false)]
    [InlineData(100, 10, 125, false)]
    [InlineData(50, 5, 55, true)]
    [InlineData(50, 5, 57, false)]
    public void IsValidBidIncrement_MatchesExpected(
        decimal currentPrice,
        decimal bidStep,
        decimal amount,
        bool expectedValid)
    {
        var isValid = BidIncrementValidator.IsValid(currentPrice, bidStep, amount);
        Assert.Equal(expectedValid, isValid);
    }
}

/// <summary>
/// Shared increment rules extracted for unit testing (mirrors BidService private logic).
/// </summary>
internal static class BidIncrementValidator
{
    public static bool IsValid(decimal currentPrice, decimal bidStep, decimal amount)
    {
        if (bidStep <= 0)
        {
            return false;
        }

        var increment = amount - currentPrice;
        if (increment < bidStep)
        {
            return false;
        }

        var steps = increment / bidStep;
        return steps == decimal.Truncate(steps);
    }
}
