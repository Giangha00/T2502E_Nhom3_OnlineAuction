using OnlineAuction.Areas.Admin.ViewModels.Categories;

namespace OnlineAuction.Services.Interfaces;

public interface ICategoryService
{
    Task<CategoryListViewModel> GetCategoriesAsync(CategoryFilterViewModel filter);

    CategoryFormViewModel BuildCreateForm();

    Task<CategoryFormViewModel?> GetEditFormAsync(int id);

    Task<CategoryDetailViewModel?> GetDetailsAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(CategoryFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(CategoryFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id);
}
