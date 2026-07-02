using OnlineAuction.Helpers;
using Xunit;

namespace OnlineAuction.Tests;

public class DashboardFilterValidatorTests
{
    [Fact]
    public void Validate_WhenDateFromAfterDateTo_ReturnsInvalid()
    {
        var result = DashboardFilterValidator.Validate(
            new DateTime(2026, 6, 10),
            new DateTime(2026, 6, 1));

        Assert.False(result.IsValid);
        Assert.Equal(DashboardFilterValidator.ErrorDateFromAfterDateTo, result.ErrorKey);
    }

    [Fact]
    public void Validate_WhenRangeExceeds365Days_ReturnsInvalid()
    {
        var dateFrom = new DateTime(2025, 1, 1);
        var dateTo = dateFrom.AddDays(365);

        var result = DashboardFilterValidator.Validate(dateFrom, dateTo);

        Assert.False(result.IsValid);
        Assert.Equal(DashboardFilterValidator.ErrorRangeTooLong, result.ErrorKey);
    }

    [Fact]
    public void Validate_WhenRangeIsExactly365Days_ReturnsValid()
    {
        var dateFrom = new DateTime(2025, 1, 1);
        var dateTo = dateFrom.AddDays(364);

        var result = DashboardFilterValidator.Validate(dateFrom, dateTo);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorKey);
    }

    [Fact]
    public void Validate_WhenDefaultSevenDayRange_ReturnsValid()
    {
        var dateTo = DateTime.UtcNow.Date;
        var dateFrom = dateTo.AddDays(-(DashboardFilterValidator.DefaultFilterDays - 1));

        var result = DashboardFilterValidator.Validate(dateFrom, dateTo);

        Assert.True(result.IsValid);
    }
}
