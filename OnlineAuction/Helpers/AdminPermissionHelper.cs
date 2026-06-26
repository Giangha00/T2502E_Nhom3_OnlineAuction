using System.Security.Claims;
using OnlineAuction.Configurations;

namespace OnlineAuction.Helpers;

public static class AdminPermissionHelper
{
    public static bool Can(ClaimsPrincipal? user, string permissionCode) =>
        user?.Identity?.IsAuthenticated == true &&
        (user.IsInRole(StaffRoleNames.Admin) ||
         user.HasClaim(PermissionClaimTypes.Permission, permissionCode));
}
