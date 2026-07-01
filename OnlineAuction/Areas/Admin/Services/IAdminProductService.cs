using OnlineAuction.Areas.Admin.ViewModels.Products;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminProductService
{
    Task<ProductTemplateListViewModel> GetProductTemplatesAsync(ProductTemplateFilterViewModel filter);

    Task<ProductListViewModel?> GetTemplateInstancesAsync(int templateId, ProductFilterViewModel filter);

    Task<ProductTemplateFormViewModel> BuildCreateTemplateFormAsync();

    Task<ProductTemplateFormViewModel?> BuildEditTemplateFormAsync(int id);

    Task<(bool Success, string Message)> CreateTemplateAsync(ProductTemplateFormViewModel model);

    Task<(bool Success, string Message)> UpdateTemplateAsync(ProductTemplateFormViewModel model);

    Task<(bool Success, string Message)> DeleteTemplateAsync(int id, int adminUserId);

    Task<ProductListViewModel> GetProductsAsync(ProductFilterViewModel filter);

    Task<ProductDetailViewModel?> GetDetailsAsync(int id);

    Task<ProductFormViewModel> BuildCreateFormAsync(int? templateId = null);

    Task<ProductFormViewModel?> BuildEditFormAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(ProductFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(ProductFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id, int adminUserId);

    Task<(bool Success, string Message)> BulkDeleteAsync(IReadOnlyList<int> productIds, int adminUserId);
}
