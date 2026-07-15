using OnlineAuction.Configurations;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class MarketplaceFeeCalculatorTests
{
    private static PlatformFeeSettings DefaultSettings => new()
    {
        RegistrationDepositPercent = 10.00m,
        BuyerCheckoutFeePercent = 2.50m,
        SellerSuccessFeePercent = 10.00m,
        MinimumRegistrationDeposit = 1.00m
    };

    [Fact]
    public void CalculateRegistrationDeposit_ReturnsTenPercentRounded()
    {
        var fee = MarketplaceFeeCalculator.CalculateRegistrationDeposit(500m, DefaultSettings);
        Assert.Equal(50.00m, fee);
    }

    [Theory]
    [InlineData(5.00, 1.00)]
    [InlineData(0.50, 1.00)]
    public void CalculateRegistrationDeposit_EnforcesMinimum(decimal productValue, decimal expectedFee)
    {
        var fee = MarketplaceFeeCalculator.CalculateRegistrationDeposit(productValue, DefaultSettings);
        Assert.Equal(expectedFee, fee);
    }

    [Fact]
    public void CalculateBuyerCheckoutFee_ReturnsTwoPointFivePercent()
    {
        var fee = MarketplaceFeeCalculator.CalculateBuyerCheckoutFee(400m, DefaultSettings);
        Assert.Equal(10.00m, fee);
    }

    [Fact]
    public void CalculateSellerSuccessFee_ReturnsTenPercent()
    {
        var fee = MarketplaceFeeCalculator.CalculateSellerSuccessFee(250m, DefaultSettings);
        Assert.Equal(25.00m, fee);
    }
}
