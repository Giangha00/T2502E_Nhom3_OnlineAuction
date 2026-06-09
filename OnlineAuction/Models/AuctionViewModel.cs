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
}
