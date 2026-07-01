using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Permissions;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PermissionService : IPermissionService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager)
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

        if (user.Role == UserRole.Admin)
        {
            return PermissionCodes.All;
        }

        return await (
                from up in _dbContext.UserPermissions.AsNoTracking()
                join permission in _dbContext.Permissions.AsNoTracking() on up.PermissionId equals permission.Id
                where up.UserId == userId
                select permission.Code)
            .Distinct()
            .OrderBy(code => code)
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

    public async Task<bool> UserHasAdminPanelAccessAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        if (user.Role == UserRole.Admin)
        {
            return true;
        }

        return await _dbContext.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetAssignedPermissionIdsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionItemViewModel>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionItemViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Description = p.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateUserPermissionsAsync(
        int userId,
        IReadOnlyList<int> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var validPermissionIds = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var existing = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.UserPermissions.RemoveRange(existing);

        foreach (var permissionId in validPermissionIds.Distinct())
        {
            _dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PermissionManagementViewModel> GetPermissionManagementViewModelAsync(
        bool canManage,
        int? selectedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionCatalogAsync(cancellationToken);

        var assignableUsers = await (
                from user in _dbContext.Users.AsNoTracking()
                where user.Role == UserRole.User && user.DeletedAt == null
                orderby user.FullName, user.Email
                select new UserPermissionRowViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    AssignedPermissionCount = _dbContext.UserPermissions.Count(up => up.UserId == user.Id)
                })
            .ToListAsync(cancellationToken);

        IReadOnlyList<int> selectedUserPermissionIds = [];
        if (selectedUserId.HasValue &&
            assignableUsers.Any(user => user.UserId == selectedUserId.Value))
        {
            selectedUserPermissionIds = await GetAssignedPermissionIdsForUserAsync(
                selectedUserId.Value,
                cancellationToken);
        }
        else
        {
            selectedUserId = null;
        }

        return new PermissionManagementViewModel
        {
            Permissions = permissions,
            AssignableUsers = assignableUsers,
            SelectedUserId = selectedUserId,
            SelectedUserPermissionIds = selectedUserPermissionIds,
            CanManage = canManage
        };
    }

    public async Task<(bool Success, string Message)> SaveUserPermissionsAsync(
        int userId,
        IReadOnlyList<int> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found.");
        }

        if (user.Role != UserRole.User)
        {
            return (false, "Only accounts with role User can receive delegated permissions. Admin accounts always have full access.");
        }

        await UpdateUserPermissionsAsync(userId, permissionIds, cancellationToken);

        return (true, $"Permissions updated for \"{user.FullName}\". They must sign out and sign in again at /Admin/Account/Login.");
    }
}
