using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionListViewModel
{
    public List<AuctionListItemViewModel> Auctions { get; set; } = [];

    public AuctionFilterViewModel Filter { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
