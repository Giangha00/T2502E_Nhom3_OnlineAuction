namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string? SetName { get; set; }

    public string? CardNumber { get; set; }

    public string? GradeLabel { get; set; }

    public string ThumbnailUrl { get; set; } = string.Empty;

    public int InstanceCount { get; set; }

    public DateTime? LastAddedAt { get; set; }
}
