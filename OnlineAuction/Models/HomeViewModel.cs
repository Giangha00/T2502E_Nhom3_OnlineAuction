namespace OnlineAuction.Models;

public class HomeViewModel
{
    public List<AuctionItemViewModel> FeaturedAuctions { get; set; } = [];
    public List<AuctionItemViewModel> WonAuctions { get; set; } = [];
    public List<SellerViewModel> BestSellers { get; set; } = [];
}

public class AuctionItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TimeRemaining { get; set; } = string.Empty;
}

public class SellerViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int AuctionCount { get; set; }
    public int SuccessfulSales { get; set; }
    public double Rating { get; set; }
}
