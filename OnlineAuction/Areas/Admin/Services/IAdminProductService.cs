using OnlineAuction.Areas.Admin.ViewModels.Products;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminProductService
{
    Task<ProductListViewModel> GetProductsAsync(ProductFilterViewModel filter);

    Task<ProductTemplateInstancesViewModel?> GetTemplateInstancesAsync(int templateId);

    Task<ProductDetailViewModel?> GetDetailsAsync(int id);

    Task<ProductFormViewModel> BuildCreateFormAsync();

    Task<ProductFormViewModel?> GetEditFormAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(ProductFormViewModel model, int? createdBy);

    Task<(bool Success, string Message)> UpdateAsync(ProductFormViewModel model, int? updatedBy);

    Task<(bool Success, string Message)> DeleteAsync(int id, int? deletedBy);

    Task PopulateFormOptionsAsync(ProductFormViewModel model);
}
