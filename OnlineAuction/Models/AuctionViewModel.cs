namespace OnlineAuction.Models;

public class AuctionViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = [];
    public List<AuctionItemViewModel> Auctions { get; set; } = [];
}

public class CategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string DisplayCount { get; set; } = string.Empty;
}
