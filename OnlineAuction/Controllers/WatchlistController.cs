using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
[Route("Watchlist")]
public class WatchlistController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWatchlistService _watchlistService;

    public WatchlistController(
        UserManager<ApplicationUser> userManager,
        IWatchlistService watchlistService)
    {
        _userManager = userManager;
        _watchlistService = watchlistService;
    }

    [HttpPost("Toggle/{auctionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int auctionId, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new { success = false, message = "Please sign in." });
        }

        try
        {
            var result = await _watchlistService.ToggleAsync(user.Id, auctionId, cancellationToken);
            return Json(new
            {
                success = true,
                isWatched = result.IsWatched,
                count = result.Count
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("Ids")]
    public async Task<IActionResult> Ids(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new { success = false });
        }

        var ids = await _watchlistService.GetWatchedAuctionIdsAsync(user.Id, cancellationToken);
        return Json(new { success = true, auctionIds = ids });
    }
}
