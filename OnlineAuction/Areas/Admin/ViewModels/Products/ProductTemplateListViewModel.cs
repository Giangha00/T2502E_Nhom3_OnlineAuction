namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateListViewModel
{
    public List<ProductTemplateListItemViewModel> Templates { get; set; } = [];

    public ProductTemplateFilterViewModel Filter { get; set; } = new();

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
