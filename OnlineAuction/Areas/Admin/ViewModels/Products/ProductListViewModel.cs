using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductListViewModel
{
    public List<ProductTemplateListItemViewModel> Templates { get; set; } = [];

    public ProductFilterViewModel Filter { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> ConditionOptions { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => Filter.Page > 1;

    public bool HasNextPage => Filter.Page < TotalPages;
}
