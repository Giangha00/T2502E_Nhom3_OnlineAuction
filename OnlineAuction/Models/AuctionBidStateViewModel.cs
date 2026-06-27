namespace OnlineAuction.Models;

public sealed class AuctionBidStateViewModel
{
    public int AuctionId { get; set; }

    public decimal CurrentPrice { get; set; }

    public int BidCount { get; set; }

    public decimal MinNextBid { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsEnded { get; set; }

    public IReadOnlyList<BidHistoryItemViewModel> BidHistory { get; set; } = [];
}
