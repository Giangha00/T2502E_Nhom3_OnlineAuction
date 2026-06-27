namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductCategoryListViewModel
{
    public List<ProductCategoryTemplateViewModel> Categories { get; set; } = [];

    public ProductCategoryFilterViewModel Filter { get; set; } = new();

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
