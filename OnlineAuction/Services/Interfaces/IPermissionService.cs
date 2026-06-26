namespace OnlineAuction.Services.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(int roleId, CancellationToken cancellationToken = default);

    Task<bool> UserHasPermissionAsync(int userId, string permissionCode, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> AssignPermissionsToRoleAsync(
        int roleId,
        IReadOnlyCollection<int> permissionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDefinition>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffRoleDefinition>> GetStaffRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetPermissionIdsForRoleAsync(int roleId, CancellationToken cancellationToken = default);
}

public sealed class PermissionDefinition
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Module { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class StaffRoleDefinition
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
