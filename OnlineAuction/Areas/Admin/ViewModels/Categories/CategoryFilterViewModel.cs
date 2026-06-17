namespace OnlineAuction.Areas.Admin.ViewModels.Categories;

public class CategoryFilterViewModel
{
    public string? Search { get; set; }

    public string? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
