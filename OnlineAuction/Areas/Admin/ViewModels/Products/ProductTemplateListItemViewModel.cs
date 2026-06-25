namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int InstanceCount { get; set; }

    public decimal? MinPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}
