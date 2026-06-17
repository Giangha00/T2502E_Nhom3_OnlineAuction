namespace OnlineAuction.Areas.Admin.ViewModels.Categories;

public class CategoryListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public int ProductCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
