namespace OnlineAuction.Models;

public class AuctionBidHistoryPageViewModel
{
    public int AuctionId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public decimal CurrentPrice { get; set; }

    public int BidCount { get; set; }

    public bool IsEnded { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public int TotalPages { get; set; } = 1;

    public IReadOnlyList<BidHistoryItemViewModel> Bids { get; set; } = [];
}
