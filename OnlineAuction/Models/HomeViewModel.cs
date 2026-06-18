namespace OnlineAuction.Models;

using OnlineAuction.Entities;

public class HomeViewModel
{
    public List<AuctionItemViewModel> HotAuctions { get; set; } = [];
    public List<AuctionItemViewModel> FeaturedAuctions { get; set; } = [];
    public List<AuctionItemViewModel> EndingSoonAuctions { get; set; } = [];
    public AuctionItemViewModel? FeaturedEndingSoon { get; set; }
    public List<AuctionItemViewModel> WonAuctions { get; set; } = [];
    public List<SellerViewModel> BestSellers { get; set; } = [];
    public List<CategoryViewModel> Categories { get; set; } = [];
    public List<VaultPostViewModel> VaultPosts { get; set; } = [];
    public int TotalLiveAuctions { get; set; }
    public int EndingSoonCount { get; set; }
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
    public string Subtitle { get; set; } = string.Empty;
    public string Condition { get; set; } = "Graded";
    public int Year { get; set; }
    public bool IsHot { get; set; }
    public int BidCount { get; set; }
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
