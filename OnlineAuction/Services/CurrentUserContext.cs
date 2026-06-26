using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetUserIdFromSchemeAsync(AuthSchemes.User, cancellationToken);
    }

    public async Task<int?> GetAdminIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetUserIdFromSchemeAsync(AuthSchemes.Admin, cancellationToken);
    }

    private async Task<int?> GetUserIdFromSchemeAsync(string scheme, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var authResult = await httpContext.AuthenticateAsync(scheme);
        if (!authResult.Succeeded)
        {
            return null;
        }

        var userIdValue = authResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
