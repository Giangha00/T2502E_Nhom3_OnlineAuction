namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AdminBidHistoryItemViewModel
{
    public int RowNumber { get; set; }

    public int BidderId { get; set; }

    public string BidderName { get; set; } = string.Empty;

    public string BidderEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime PlacedAt { get; set; }

    public string BidType { get; set; } = string.Empty;

    public string Status { get; set; } = "OUTBID";

    public bool IsWinning { get; set; }
}
