using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowListViewModel
{
    public IReadOnlyList<BuyNowListItemViewModel> Listings { get; set; } = Array.Empty<BuyNowListItemViewModel>();

    public BuyNowFilterViewModel Filter { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public List<SelectListItem> SellerOptions { get; set; } = new();

    public List<SelectListItem> StatusFilterOptions { get; set; } = new();

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool CanManage { get; set; }
}
