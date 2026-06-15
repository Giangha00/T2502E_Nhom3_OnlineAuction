namespace OnlineAuction.Models;

public class AuctionCardViewModel
{
    public AuctionItemViewModel Item { get; set; } = new();
    public bool EnableFiltering { get; set; }
    public bool ShowBidLink { get; set; } = true;
    public string Variant { get; set; } = "grid";
}
