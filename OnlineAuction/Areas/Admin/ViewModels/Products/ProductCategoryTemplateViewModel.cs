namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductCategoryTemplateViewModel
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public string ThumbnailUrl { get; set; } = string.Empty;

    public DateTime? LatestProductCreatedAt { get; set; }
}
