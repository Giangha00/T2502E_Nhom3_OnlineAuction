namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductListItemViewModel
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string? GradeLabel { get; set; }

    public string? CardNumber { get; set; }

    public string? CertNumber { get; set; }

    public decimal? EstimatedValue { get; set; }

    public decimal? ImportPrice { get; set; }

    public int AuctionCount { get; set; }

    public bool CanDelete { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
