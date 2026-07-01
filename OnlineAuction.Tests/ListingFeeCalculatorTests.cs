using OnlineAuction.Configurations;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class ListingFeeCalculatorTests
{
    [Fact]
    public void CalculateListingFee_Fixed_ReturnsConfiguredAmount()
    {
        var settings = new PlatformFeeSettings
        {
            ListingFeeType = ListingFeeTypes.Fixed,
            ListingFeeAmount = 5.00m
        };

        var fee = ListingFeeCalculator.CalculateListingFee(settings, startingPrice: 500m);

        Assert.Equal(5.00m, fee);
    }

    [Fact]
    public void CalculateListingFee_Percent_ReturnsRoundedPercentage()
    {
        var settings = new PlatformFeeSettings
        {
            ListingFeeType = ListingFeeTypes.Percent,
            ListingFeePercent = 2.00m
        };

        var fee = ListingFeeCalculator.CalculateListingFee(settings, startingPrice: 500m);

        Assert.Equal(10.00m, fee);
    }

    [Theory]
    [InlineData(10, 1.00)]
    [InlineData(20, 1.00)]
    [InlineData(100, 2.00)]
    public void CalculateListingFee_Percent_EnforcesMinimumOneDollar(decimal startingPrice, decimal expectedFee)
    {
        var settings = new PlatformFeeSettings
        {
            ListingFeeType = ListingFeeTypes.Percent,
            ListingFeePercent = 2.00m
        };

        var fee = ListingFeeCalculator.CalculateListingFee(settings, startingPrice);

        Assert.Equal(expectedFee, fee);
    }
}
