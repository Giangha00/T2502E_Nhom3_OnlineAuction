using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionFilterViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? ListingPhase { get; set; }

    public string? ListingType { get; set; } = ListingTypes.Auction;

    public int? CategoryId { get; set; }

    public string? SortOrder { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
