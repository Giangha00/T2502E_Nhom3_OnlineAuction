namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductBulkDeleteViewModel
{
    public List<int> SelectedProductIds { get; set; } = [];

    public int? ReturnCategoryId { get; set; }
}
