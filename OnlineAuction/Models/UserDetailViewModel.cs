namespace OnlineAuction.Models;

public class UserDetailViewModel
{
    public SellerViewModel Seller { get; set; } = new();
    public List<AuctionItemViewModel> ActiveListings { get; set; } = [];
}
