namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowListItemViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public decimal BuyNowPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AvailabilityLabel { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public bool IsPublicLive { get; set; }

    public bool CanEdit { get; set; }
}
