using System.Security.Claims;
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

        if (user.Role == UserRole.Admin || await _userManager.IsInRoleAsync(user, StaffRoleNames.Admin))
        {
            return PermissionCodes.All;
        }

        var claims = await _userManager.GetClaimsAsync(user);
        return claims
            .Where(claim => claim.Type == PermissionClaimTypes.Permission
                            && !string.IsNullOrWhiteSpace(claim.Value)
                            && PermissionCatalog.IsKnown(claim.Value))
            .Select(claim => claim.Value!)
            .Distinct()
            .OrderBy(code => code)
            .ToList();
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

        if (user.Role == UserRole.Admin || await _userManager.IsInRoleAsync(user, StaffRoleNames.Admin))
        {
            return true;
        }

        var claims = await _userManager.GetClaimsAsync(user);
        return claims.Any(claim =>
            claim.Type == PermissionClaimTypes.Permission
            && !string.IsNullOrWhiteSpace(claim.Value)
            && PermissionCatalog.IsKnown(claim.Value));
    }

    public async Task<IReadOnlyList<string>> GetAssignedPermissionCodesForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        var claims = await _userManager.GetClaimsAsync(user);
        return claims
            .Where(claim => claim.Type == PermissionClaimTypes.Permission
                            && !string.IsNullOrWhiteSpace(claim.Value)
                            && PermissionCatalog.IsKnown(claim.Value))
            .Select(claim => claim.Value!)
            .Distinct()
            .OrderBy(code => code)
            .ToList();
    }

    public Task<IReadOnlyList<PermissionItemViewModel>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PermissionCatalog.All);
    }

    public async Task UpdateUserPermissionsAsync(
        int userId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        var desired = permissionCodes
            .Where(PermissionCatalog.IsKnown)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var existing = (await _userManager.GetClaimsAsync(user))
            .Where(claim => claim.Type == PermissionClaimTypes.Permission)
            .ToList();

        foreach (var claim in existing.Where(claim =>
                     string.IsNullOrWhiteSpace(claim.Value) || !desired.Contains(claim.Value)))
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        var existingValues = existing
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Value))
            .Select(claim => claim.Value!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var code in desired.Where(code => !existingValues.Contains(code)))
        {
            await _userManager.AddClaimAsync(
                user,
                new Claim(PermissionClaimTypes.Permission, code));
        }
    }

    public async Task<PermissionManagementViewModel> GetPermissionManagementViewModelAsync(
        bool canManage,
        int? selectedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionCatalogAsync(cancellationToken);

        var assignableUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.User && user.DeletedAt == null)
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .Select(user => new UserPermissionRowViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        if (assignableUsers.Count > 0)
        {
            var userIds = assignableUsers.Select(user => user.UserId).ToList();
            var knownCodes = PermissionCodes.All;
            var claimRows = await _dbContext.Set<IdentityUserClaim<int>>()
                .AsNoTracking()
                .Where(claim =>
                    claim.ClaimType == PermissionClaimTypes.Permission
                    && userIds.Contains(claim.UserId)
                    && claim.ClaimValue != null)
                .Select(claim => new { claim.UserId, claim.ClaimValue })
                .ToListAsync(cancellationToken);

            var counts = claimRows
                .Where(claim => knownCodes.Contains(claim.ClaimValue!))
                .GroupBy(claim => claim.UserId)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (var row in assignableUsers)
            {
                row.AssignedPermissionCount = counts.GetValueOrDefault(row.UserId);
            }
        }

        IReadOnlyList<string> selectedUserPermissionCodes = [];
        if (selectedUserId.HasValue &&
            assignableUsers.Any(user => user.UserId == selectedUserId.Value))
        {
            selectedUserPermissionCodes = await GetAssignedPermissionCodesForUserAsync(
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
            SelectedUserPermissionCodes = selectedUserPermissionCodes,
            CanManage = canManage
        };
    }

    public async Task<(bool Success, string Message)> SaveUserPermissionsAsync(
        int userId,
        IReadOnlyList<string> permissionCodes,
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

        await UpdateUserPermissionsAsync(userId, permissionCodes, cancellationToken);

        return (true, $"Permissions updated for \"{user.FullName}\". They must sign out and sign in again at /Admin/Account/Login.");
    }
}
