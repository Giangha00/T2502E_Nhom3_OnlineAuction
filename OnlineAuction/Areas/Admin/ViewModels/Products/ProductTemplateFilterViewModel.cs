namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateFilterViewModel
{
    public string? Search { get; set; }

    public string? SortOrder { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
