using OnlineAuction.Areas.Admin.ViewModels.Users;
using AdminUserDetailViewModel = OnlineAuction.Areas.Admin.ViewModels.Users.UserDetailViewModel;
using PublicUserDetailViewModel = OnlineAuction.Models.UserDetailViewModel;

namespace OnlineAuction.Services.Interfaces;

public interface IUserService
{
    Task<PublicUserDetailViewModel?> GetPublicProfileAsync(int id, int? viewerUserId = null);

    Task<UserListViewModel> GetUsersAsync(UserFilterViewModel filter);

    Task<UserFormViewModel> BuildCreateFormAsync();

    Task<UserFormViewModel?> GetEditFormAsync(int id);

    Task<AdminUserDetailViewModel?> GetDetailsAsync(int id);

    Task<(bool Success, string Message)> CreateAsync(UserFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(UserFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id);

    Task<(bool Success, string Message)> ExecuteBulkActionAsync(UserBulkActionViewModel model);
}