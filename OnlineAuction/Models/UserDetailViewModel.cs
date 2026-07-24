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
    public List<AuctionItemViewModel> RelatedAuctions { get; set; } = [];
}

public class UserProfileViewModel
{
    public int Id { get; set; }
    public bool IsOwner { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public int MemberSince { get; set; }
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
    public bool IsOwner { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool CanViewContactInfo { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;
}

public class SellerStatisticsViewModel
{
    public int TotalListings { get; set; }

    public int TotalAuctions { get; set; }

    public int TotalBuyNowListings { get; set; }

    public int CompletedAuctions { get; set; }

    public int TotalSales { get; set; }

    public decimal GrossSales { get; set; }

    public decimal SellerFees { get; set; }

    public decimal NetProceeds { get; set; }
}

public class SellerListingCardViewModel
{
    public AuctionItemViewModel Auction { get; set; } = new();

    public bool IsOwner { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}

public class EmptyListingStateViewModel
{
    public bool IsOwner { get; set; }

    public string Channel { get; set; } = ListingTypes.Auction;
}
