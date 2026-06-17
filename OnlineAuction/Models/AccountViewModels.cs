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

public class SellingViewModel
{
    public AccountShellViewModel Shell { get; set; } = new();
    public string Tab { get; set; } = "active";
    public string Channel { get; set; } = "buynow";
    public List<AuctionItemViewModel> Listings { get; set; } = [];
}
