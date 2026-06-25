namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateInstancesViewModel
{
    public int TemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int InstanceCount { get; set; }

    public List<ProductListItemViewModel> Instances { get; set; } = [];
}
