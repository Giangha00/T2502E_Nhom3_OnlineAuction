using System.Security.Claims;
using OnlineAuction.Configurations;

namespace OnlineAuction.Helpers;

public static class AdminPermissionHelper
{
    public static bool IsSuperAdmin(ClaimsPrincipal? user) => AdminAccessHelper.IsFullAdmin(user);

    public static bool Can(ClaimsPrincipal? user, string permissionCode) =>
        AdminAccessHelper.Can(user, permissionCode);
}
