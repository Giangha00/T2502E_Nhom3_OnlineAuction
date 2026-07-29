using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionFilterViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? ListingPhase { get; set; }

    public string? DateRange { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Null/empty = all listing types. Defaults to auction-only when browsing /Admin/Auction.
    /// Dashboard KPI links pass empty to include buy-now rows in status totals.
    /// </summary>
    public string? ListingType { get; set; } = ListingTypes.Auction;

    public int? CategoryId { get; set; }

    public string? SortOrder { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
