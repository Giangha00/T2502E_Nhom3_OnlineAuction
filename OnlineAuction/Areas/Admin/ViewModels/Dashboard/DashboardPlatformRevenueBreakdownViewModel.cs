namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardPlatformRevenueBreakdownViewModel
{
    public decimal ListingFees { get; set; }

    public decimal TransactionCommission { get; set; }

    public decimal ListingFeePercentage { get; set; }

    public decimal TransactionCommissionPercentage { get; set; }

    public bool HasTransactionCommission => TransactionCommission > 0;
}
