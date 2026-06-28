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
    private readonly IWatchlistService _watchlistService;
    private readonly IUserAccountService _userAccountService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ISellerAuctionService sellerAuctionService,
        IWatchlistService watchlistService,
        IUserAccountService userAccountService)
    {
        _userManager = userManager;
        _localizer = localizer;
        _sellerAuctionService = sellerAuctionService;
        _watchlistService = watchlistService;
        _userAccountService = userAccountService;
    }

    public async Task<IActionResult> Bids(string tab = "active")
    {
        var shell = await BuildShellAsync("bids");
        var user = await _userManager.GetUserAsync(User);
        var normalizedTab = tab.ToLowerInvariant() switch
        {
            "past" => "past",
            _ => "active"
        };
        var listings = user is null
            ? []
            : await _userAccountService.GetUserBidsAsync(user.Id, normalizedTab);

        return View("AccountListings", new AccountListingsViewModel
        {
            Shell = shell,
            PageTitle = _localizer["Account_Bids"],
            PageDescription = _localizer["Account_Bids_Desc"],
            Listings = listings,
            ActiveTab = normalizedTab,
            Tabs =
            [
                ("active", _localizer["Account_Bids_Tab_Active"].Value),
                ("past", _localizer["Account_Bids_Tab_Past"].Value)
            ],
            CardMode = "auction",
            EmptyDesc = _localizer["Account_Bids_Empty"].Value
        });
    }

    public async Task<IActionResult> Watchlist()
    {
        var shell = await BuildShellAsync("watchlist");
        var user = await _userManager.GetUserAsync(User);
        var listings = user is null
            ? []
            : await _watchlistService.GetItemsAsync(user.Id);
        var watchedIds = user is null
            ? new HashSet<int>()
            : await _watchlistService.GetWatchedAuctionIdsAsync(user.Id);

        return View("AccountListings", new AccountListingsViewModel
        {
            Shell = shell,
            PageTitle = _localizer["Account_Watchlist"],
            PageDescription = _localizer["Account_Watchlist_Desc"],
            Listings = listings,
            ShowWatchlistButton = true,
            WatchedAuctionIds = watchedIds,
            EmptyDesc = _localizer["Account_Watchlist_Empty"].Value
        });
    }

    public async Task<IActionResult> Offers()
    {
        var shell = await BuildShellAsync("offers");
        var user = await _userManager.GetUserAsync(User);
        var listings = user is null
            ? []
            : await _userAccountService.GetUserOffersAsync(user.Id);

        return View("AccountListings", new AccountListingsViewModel
        {
            Shell = shell,
            PageTitle = _localizer["Account_Offers"],
            PageDescription = _localizer["Account_Offers_Desc"],
            Listings = listings,
            CardMode = "buynow",
            ShowBidLink = false,
            EmptyDesc = _localizer["Account_Offers_Empty"].Value
        });
    }

    public async Task<IActionResult> Selling(string tab = "active", string channel = "buynow")
    {
        var shell = await BuildShellAsync("selling");
        var normalizedTab = NormalizeTab(tab);
        var normalizedChannel = NormalizeChannel(channel);
        var user = await _userManager.GetUserAsync(User);
        var listings = user is null
            ? []
            : await _sellerAuctionService.GetSellerAuctionsAsync(
                user.Id,
                normalizedChannel,
                tab: normalizedTab);

        return View(new SellingViewModel
        {
            Shell = shell,
            Tab = normalizedTab,
            Channel = normalizedChannel,
            Listings = listings
        });
    }

    public async Task<IActionResult> Submissions()
    {
        var shell = await BuildShellAsync("submissions");
        var user = await _userManager.GetUserAsync(User);
        var listings = user is null
            ? []
            : await _userAccountService.GetUserSubmissionsAsync(user.Id);

        return View("AccountListings", new AccountListingsViewModel
        {
            Shell = shell,
            PageTitle = _localizer["Account_Submissions"],
            PageDescription = _localizer["Account_Submissions_Desc"],
            Listings = listings,
            ShowBidLink = false,
            ShowTimeRemaining = false,
            EmptyDesc = _localizer["Account_Submissions_Empty"].Value
        });
    }

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
