namespace OnlineAuction.Models;

public class AccountShellViewModel
{
    public string ActiveSection { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Initials { get; set; } = "U";
    public string VaultAddressName { get; set; } = string.Empty;
    public string VaultAddressLine1 { get; set; } = string.Empty;
    public string VaultAddressLine2 { get; set; } = string.Empty;
    public string VaultId { get; set; } = string.Empty;
}

public class AccountPageViewModel
{
    public AccountShellViewModel Shell { get; set; } = new();
    public string PageTitle { get; set; } = string.Empty;
    public string PageDescription { get; set; } = string.Empty;
}

public class AccountListingsViewModel
{
    public AccountShellViewModel Shell { get; set; } = new();
    public string PageTitle { get; set; } = string.Empty;
    public string PageDescription { get; set; } = string.Empty;
    public List<AuctionItemViewModel> Listings { get; set; } = [];
    public string? ActiveTab { get; set; }
    public IReadOnlyList<(string Key, string Label)>? Tabs { get; set; }
    public string CardMode { get; set; } = "auction";
    public bool ShowWatchlistButton { get; set; }
    public bool ShowBidLink { get; set; } = true;
    public bool ShowTimeRemaining { get; set; } = true;
    public IReadOnlySet<int> WatchedAuctionIds { get; set; } = new HashSet<int>();
    public string EmptyTitleKey { get; set; } = "Account_Empty_Title";
    public string? EmptyDesc { get; set; }
}

public class SellingViewModel
{
    public AccountShellViewModel Shell { get; set; } = new();
    public string Tab { get; set; } = "active";
    public string Channel { get; set; } = "buynow";
    public List<AuctionItemViewModel> Listings { get; set; } = [];
}
