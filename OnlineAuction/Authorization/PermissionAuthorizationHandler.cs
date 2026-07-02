using Microsoft.AspNetCore.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;

namespace OnlineAuction.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (AdminAccessHelper.IsFullAdmin(context.User) ||
            context.User.HasClaim(PermissionClaimTypes.Permission, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
