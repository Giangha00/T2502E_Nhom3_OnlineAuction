namespace OnlineAuction.Areas.Admin.ViewModels.Categories;

public class CategoryListViewModel
{
    public List<CategoryListItemViewModel> Categories { get; set; } = [];

    public CategoryFilterViewModel Filter { get; set; } = new();

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => Filter.Page > 1;

    public bool HasNextPage => Filter.Page < TotalPages;
}
