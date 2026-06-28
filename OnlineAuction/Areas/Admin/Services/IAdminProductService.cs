using OnlineAuction.Areas.Admin.ViewModels.Products;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminProductService
{
    Task<ProductCategoryListViewModel> GetCategoryTemplatesAsync(ProductCategoryFilterViewModel filter);

    Task<ProductListViewModel?> GetCategoryProductsAsync(int categoryId, ProductFilterViewModel filter);

    Task<ProductListViewModel> GetProductsAsync(ProductFilterViewModel filter);

    Task<ProductDetailViewModel?> GetDetailsAsync(int id);

    Task<ProductFormViewModel> BuildCreateFormAsync();

    Task<ProductFormViewModel?> BuildEditFormAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(ProductFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(ProductFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id, int adminUserId);

    Task<(bool Success, string Message)> BulkDeleteAsync(IReadOnlyList<int> productIds, int adminUserId);
}
