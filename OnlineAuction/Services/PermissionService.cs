using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PermissionService : IPermissionService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly UserManager<Entities.ApplicationUser> _userManager;

    public PermissionService(
        AuctionHouseDbContext dbContext,
        UserManager<Entities.ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(StaffRoleNames.Admin))
        {
            return PermissionCodes.All;
        }

        var roleIds = await _dbContext.Roles.AsNoTracking()
            .Where(role => roles.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        var roleName = await _dbContext.Roles.AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role => role.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleName == StaffRoleNames.Admin)
        {
            return PermissionCodes.All;
        }

        return await _dbContext.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserHasPermissionAsync(
        int userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsForUserAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public async Task<(bool Success, string Message)> AssignPermissionsToRoleAsync(
        int roleId,
        IReadOnlyCollection<int> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            return (false, "Role not found.");
        }

        if (role.Name == StaffRoleNames.Admin)
        {
            return (false, "Admin role permissions are managed automatically.");
        }

        var validPermissionIds = await _dbContext.Permissions.AsNoTracking()
            .Where(permission => permissionIds.Contains(permission.Id))
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);

        var existing = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in validPermissionIds)
        {
            _dbContext.RolePermissions.Add(new Entities.RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, "Role permissions updated successfully.");
    }

    public async Task<IReadOnlyList<PermissionDefinition>> GetAllPermissionsAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Permissions.AsNoTracking()
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Name)
            .Select(permission => new PermissionDefinition
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                Module = permission.Module,
                Description = permission.Description
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StaffRoleDefinition>> GetStaffRolesAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Roles.AsNoTracking()
            .Where(role => role.Name != null && StaffRoleNames.All.Contains(role.Name))
            .OrderBy(role => role.Name)
            .Select(role => new StaffRoleDefinition
            {
                Id = role.Id,
                Name = role.Name!
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetPermissionIdsForRoleAsync(
        int roleId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
}
