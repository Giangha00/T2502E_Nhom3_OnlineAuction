namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PrimaryImage { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string? CardNumber { get; set; }

    public string? CertNumber { get; set; }

    public int ImageCount { get; set; }

    public int AuctionCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
