namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductCategoryFilterViewModel
{
    public string? Search { get; set; }

    public string? SortOrder { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
