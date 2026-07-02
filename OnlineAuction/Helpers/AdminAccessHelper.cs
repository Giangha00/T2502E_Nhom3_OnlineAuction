using System.Security.Claims;
using OnlineAuction.Configurations;
using OnlineAuction.Enums;

namespace OnlineAuction.Helpers;

public static class AdminAccessHelper
{
    public static bool IsFullAdmin(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true &&
        (user.HasClaim(PermissionClaimTypes.SuperAdmin, bool.TrueString) ||
         user.HasClaim(PermissionClaimTypes.AppRole, UserRole.Admin.ToString()) ||
         user.IsInRole(StaffRoleNames.Admin));

    public static bool Can(ClaimsPrincipal? user, string permissionCode) =>
        user?.Identity?.IsAuthenticated == true &&
        (IsFullAdmin(user) || user.HasClaim(PermissionClaimTypes.Permission, permissionCode));
}
