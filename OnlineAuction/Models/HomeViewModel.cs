namespace OnlineAuction.Models;

using OnlineAuction.Entities;

public class HomeViewModel
{
    public List<AuctionItemViewModel> Recommended { get; set; } = [];
    public List<AuctionItemViewModel> TrendingOnAuction { get; set; } = [];
    public List<AuctionItemViewModel> TrendingOnBuyNow { get; set; } = [];
    public List<AuctionItemViewModel> RecentlyAdded { get; set; } = [];
    public List<SellerViewModel> BestSellers { get; set; } = [];
    public List<CategoryViewModel> Categories { get; set; } = [];
    public List<VaultPostViewModel> VaultPosts { get; set; } = [];
}

public class HomeProductSectionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string SectionKey { get; set; } = string.Empty;
    public List<AuctionItemViewModel> Items { get; set; } = [];
    public string CardMode { get; set; } = "auction";
    public string ViewAllController { get; set; } = "Auction";
    public string ViewAllAction { get; set; } = "Index";
    public bool ShowViewAll { get; set; } = true;
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
    public string ListingType { get; set; } = ListingTypes.Auction;
    public string Grade { get; set; } = string.Empty;
    public string Authenticator { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Condition { get; set; } = "Graded";
    public int Year { get; set; }
    public bool IsHot { get; set; }
    public int BidCount { get; set; }
    public string DealLabel { get; set; } = string.Empty;
    public string DealNote { get; set; } = string.Empty;
    public string DisplayTitle { get; set; } = string.Empty;
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
