using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Resources;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ISellerAuctionService _sellerAuctionService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ISellerAuctionService sellerAuctionService)
    {
        _userManager = userManager;
        _localizer = localizer;
        _sellerAuctionService = sellerAuctionService;
    }

    public Task<IActionResult> Bids() =>
        PageAsync("bids", _localizer["Account_Bids"], _localizer["Account_Bids_Desc"]);

    public Task<IActionResult> Watchlist() =>
        PageAsync("watchlist", _localizer["Account_Watchlist"], _localizer["Account_Watchlist_Desc"]);

    public Task<IActionResult> Offers() =>
        PageAsync("offers", _localizer["Account_Offers"], _localizer["Account_Offers_Desc"]);

    public async Task<IActionResult> Selling(string tab = "active", string channel = "buynow")
    {
        var shell = await BuildShellAsync("selling");
        var normalizedTab = NormalizeTab(tab);
        var normalizedChannel = NormalizeChannel(channel);
        var user = await _userManager.GetUserAsync(User);
        var listings = user is null
            ? []
            : await _sellerAuctionService.GetSellerAuctionsAsync(user.Id, normalizedChannel);

        return View(new SellingViewModel
        {
            Shell = shell,
            Tab = normalizedTab,
            Channel = normalizedChannel,
            Listings = listings
        });
    }

    public Task<IActionResult> Summary() =>
        PageAsync("summary", _localizer["Account_Summary"], _localizer["Account_Summary_Desc"]);

    public Task<IActionResult> Accounting() =>
        PageAsync("accounting", _localizer["Account_Accounting"], _localizer["Account_Accounting_Desc"]);

    public Task<IActionResult> Submissions() =>
        PageAsync("submissions", _localizer["Account_Submissions"], _localizer["Account_Submissions_Desc"]);

    public Task<IActionResult> Preferences() =>
        PageAsync("preferences", _localizer["Account_Preferences"], _localizer["Account_Preferences_Desc"]);

    private async Task<IActionResult> PageAsync(string section, string title, string description)
    {
        var shell = await BuildShellAsync(section);
        return View("AccountPage", new AccountPageViewModel
        {
            Shell = shell,
            PageTitle = title,
            PageDescription = description
        });
    }
private async Task<AccountShellViewModel> BuildShellAsync(string activeSection)
    {
        var user = await _userManager.GetUserAsync(User);
        var displayName = user?.FullName ?? User.Identity?.Name ?? "User";
        var userId = user?.Id ?? 0;
        var vaultId = userId > 0 ? (11600000 + userId).ToString() : "11608867";

        return new AccountShellViewModel
        {
            ActiveSection = activeSection,
            UserId = userId,
            DisplayName = displayName,
            Initials = GetInitials(displayName),
            VaultAddressName = displayName,
            VaultAddressLine1 = $"7560 SW Durham Rd, ID {vaultId}",
            VaultAddressLine2 = "Tigard, OR 97224",
            VaultId = vaultId
        };
    }

    private static string GetInitials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        return string.IsNullOrWhiteSpace(name) ? "U" : char.ToUpperInvariant(name[0]).ToString();
    }

    private static string NormalizeTab(string tab) =>
        tab.ToLowerInvariant() switch
        {
            "sold" => "sold",
            "unsold" => "unsold",
            "scheduled" => "scheduled",
            _ => "active"
        };

    private static string NormalizeChannel(string channel) =>
        channel.ToLowerInvariant() switch
        {
            "auction" => "auction",
            _ => "buynow"
        };
}