using OnlineAuction.Areas.Admin.ViewModels.Permissions;

namespace OnlineAuction.Services.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UserHasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken = default);

    Task<bool> UserHasAdminPanelAccessAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAssignedPermissionCodesForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionItemViewModel>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default);

    Task UpdateUserPermissionsAsync(int userId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);

    Task<PermissionManagementViewModel> GetPermissionManagementViewModelAsync(
        bool canManage,
        int? selectedUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> SaveUserPermissionsAsync(
        int userId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken = default);
}
