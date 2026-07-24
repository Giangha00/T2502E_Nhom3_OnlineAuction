using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowListViewModel
{
    public IReadOnlyList<BuyNowListItemViewModel> Listings { get; set; } = [];

    public BuyNowFilterViewModel Filter { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool CanManage { get; set; }
}
