namespace OnlineAuction.Services.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UserHasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken = default);
}
