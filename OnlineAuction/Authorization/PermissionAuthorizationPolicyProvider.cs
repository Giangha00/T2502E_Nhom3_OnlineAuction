using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;

namespace OnlineAuction.Authorization;

/// <summary>
/// Creates permission policies at runtime instead of registering every code in Program.cs.
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionCodes.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionCode = policyName[PermissionCodes.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder(AuthSchemes.Admin)
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
