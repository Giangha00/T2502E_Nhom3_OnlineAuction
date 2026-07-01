using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;

public class AuctionVerificationFilterViewModel
{
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public string? ListingType { get; set; }

    public string? DateRange { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AuctionVerificationListViewModel
{
    public IReadOnlyList<AuctionVerificationListItemViewModel> Items { get; set; } = [];

    public AuctionVerificationFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}

public class AuctionVerificationListItemViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string ListingType { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}
