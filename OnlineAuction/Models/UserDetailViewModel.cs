namespace OnlineAuction.Models;

using OnlineAuction.Entities;

public class UserDetailViewModel
{
    public bool IsOwner { get; set; }

    public UserProfileViewModel Profile { get; set; } = new();
    public UserBasicInfoViewModel BasicInfo { get; set; } = new();
    public SellerStatisticsViewModel Statistics { get; set; } = new();
    public List<AuctionItemViewModel> Auctions { get; set; } = [];
    public List<AuctionItemViewModel> BuyNowListings { get; set; } = [];
    public SellerRatingViewModel Rating { get; set; } = new();
    public List<AuctionItemViewModel> RelatedAuctions { get; set; } = [];
}

public class UserProfileViewModel
{
    public int Id { get; set; }
    public bool IsOwner { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Role { get; set; } = "Seller";
    public int MemberSince { get; set; }
}

public class SellerAuctionCardViewModel
{
    public AuctionItemViewModel Auction { get; set; } = new();
    public bool IsOwner { get; set; }
}

public class SellerListingListViewModel
{
    public bool IsOwner { get; set; }
    public string Channel { get; set; } = ListingTypes.Auction;
    public string SectionId { get; set; } = "seller-auctions";
    public List<AuctionItemViewModel> Listings { get; set; } = [];
}

public class UserBasicInfoViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SellerStatisticsViewModel
{
    public int TotalAuctions { get; set; }
    public int CompletedAuctions { get; set; }
    public int TotalSales { get; set; }
    public double Rating { get; set; }
}

public class SellerRatingViewModel
{
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<SellerReviewViewModel> Reviews { get; set; } = [];
}

public class SellerReviewViewModel
{
    public string ReviewerName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
}
