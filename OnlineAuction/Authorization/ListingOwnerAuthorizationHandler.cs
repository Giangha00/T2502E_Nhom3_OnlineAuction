using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;

namespace OnlineAuction.Authorization;

public sealed class ListingOwnerAuthorizationHandler : AuthorizationHandler<ListingOwnerRequirement>
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ListingOwnerAuthorizationHandler(
        AuctionHouseDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ListingOwnerRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (!httpContext.Request.RouteValues.TryGetValue("auctionId", out var auctionIdValue)
            && !httpContext.Request.RouteValues.TryGetValue("id", out auctionIdValue))
        {
            return;
        }

        if (!int.TryParse(auctionIdValue?.ToString(), out var auctionId))
        {
            return;
        }

        var isOwner = await _dbContext.Auctions.AsNoTracking()
            .AnyAsync(auction =>
                auction.Id == auctionId
                && auction.DeletedAt == null
                && auction.Product.SellerId == userId);

        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}
