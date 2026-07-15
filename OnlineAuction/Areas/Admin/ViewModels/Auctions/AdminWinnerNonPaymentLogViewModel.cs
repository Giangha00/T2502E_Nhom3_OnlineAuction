namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AdminWinnerNonPaymentLogViewModel
{
    public long Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public int DefaultingUserId { get; set; }

    public decimal? ForfeitedAmount { get; set; }

    public int? SecondChanceUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdminForfeitedDepositViewModel
{
    public long DepositId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime? ForfeitedAt { get; set; }
}
