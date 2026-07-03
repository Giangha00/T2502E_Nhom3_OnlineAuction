namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardPlatformRevenueBreakdownViewModel
{
    public decimal RegistrationDeposits { get; set; }

    public decimal RegistrationDepositsPercentage { get; set; }

    public decimal BuyerCheckoutFees { get; set; }

    public decimal BuyerCheckoutFeesPercentage { get; set; }

    public decimal SellerSuccessFees { get; set; }

    public decimal SellerSuccessFeesPercentage { get; set; }

    public bool HasRegistrationDeposits => RegistrationDeposits > 0;

    public bool HasBuyerCheckoutFees => BuyerCheckoutFees > 0;

    public bool HasSellerSuccessFees => SellerSuccessFees > 0;
}
