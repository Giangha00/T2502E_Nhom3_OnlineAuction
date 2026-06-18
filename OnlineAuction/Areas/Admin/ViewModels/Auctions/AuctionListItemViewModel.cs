namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionListItemViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ListingType { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int BidCount { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
