using Microsoft.AspNetCore.Authorization;
using OnlineAuction.Configurations;

namespace OnlineAuction.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        Policy = PermissionCodes.ToPolicyName(permissionCode);
        AuthenticationSchemes = AuthSchemes.Admin;
    }
}
