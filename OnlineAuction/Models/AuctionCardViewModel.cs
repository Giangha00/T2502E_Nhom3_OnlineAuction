namespace OnlineAuction.Models;

public class AuctionCardViewModel
{
    public AuctionItemViewModel Item { get; set; } = new();
    public bool EnableFiltering { get; set; }
    public bool ShowBidLink { get; set; } = true;
    public bool ShowWatchlistButton { get; set; } = true;
    public bool ShowTimeRemaining { get; set; } = true;
    public bool ShowProductMeta { get; set; }
    public string Variant { get; set; } = "grid";
    public string CardMode { get; set; } = "auction";
    public string PriceLabelResourceKey { get; set; } = "Card_CurrentBid";
}
