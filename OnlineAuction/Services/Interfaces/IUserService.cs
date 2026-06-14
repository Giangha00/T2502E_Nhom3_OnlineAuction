using OnlineAuction.Areas.Admin.ViewModels.Users;

namespace OnlineAuction.Services.Interfaces;

public interface IUserService
{
    
    Task<UserListViewModel> GetUsersAsync(UserFilterViewModel filter);

    UserFormViewModel BuildCreateForm();

    Task<UserFormViewModel?> GetEditFormAsync(int id);

    Task<UserDetailViewModel?> GetDetailsAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(UserFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(UserFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id);

    Task<(bool Success, string Message)> ExecuteBulkActionAsync(UserBulkActionViewModel model);
}