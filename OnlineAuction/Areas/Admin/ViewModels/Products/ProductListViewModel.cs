using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductListViewModel
{
    public List<ProductListItemViewModel> Products { get; set; } = [];

    public ProductFilterViewModel Filter { get; set; } = new();

    public int? ContextCategoryId { get; set; }

    public string? ContextCategoryName { get; set; }

    public int? ContextTemplateId { get; set; }

    public string? ContextTemplateName { get; set; }

    public ProductTemplateDetailViewModel? ContextTemplate { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> ConditionOptions { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
