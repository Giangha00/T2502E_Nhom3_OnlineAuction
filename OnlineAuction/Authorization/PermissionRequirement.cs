using Microsoft.AspNetCore.Authorization;

namespace OnlineAuction.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
