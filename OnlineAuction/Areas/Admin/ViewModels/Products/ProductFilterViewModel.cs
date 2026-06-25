namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductFilterViewModel
{
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public int? SellerId { get; set; }

    public string? Condition { get; set; }

    public string? DateRange { get; set; }

    public string? SortOrder { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
