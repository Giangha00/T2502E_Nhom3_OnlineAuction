namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowDetailViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public decimal BuyNowPrice { get; set; }

    public decimal StartingPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AvailabilityLabel { get; set; } = string.Empty;

    public bool IsPublicLive { get; set; }

    public bool CanEdit { get; set; }

    public bool CanCancel { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? ImageUrl { get; set; }

    public IReadOnlyList<string> GalleryImages { get; set; } = [];

    public IReadOnlyList<BuyNowDocumentViewModel> Documents { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? VerifierName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<BuyNowOrderSummaryViewModel> RelatedOrders { get; set; } = [];

    public bool CanManage { get; set; }
}
