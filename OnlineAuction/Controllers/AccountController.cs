using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Entities;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Wallet()
    {
        var shell = await BuildShellAsync("wallet");
        return View(shell);
    }

    public async Task<IActionResult> Orders()
    {
        var shell = await BuildShellAsync("orders");
        return View(shell);
    }

    public Task<IActionResult> Bids() =>
        PageAsync("bids", "Bids", "View your active and past auction bids.");

    public Task<IActionResult> Watchlist() =>
        PageAsync("watchlist", "Watchlist", "Keep tabs on saved items.");

    public Task<IActionResult> Offers() =>
        PageAsync("offers", "Offers", "Track offers you've made or received.");

    public async Task<IActionResult> Selling(string tab = "active", string channel = "buynow")
    {
        var shell = await BuildShellAsync("selling");
        var normalizedTab = NormalizeTab(tab);
        var normalizedChannel = NormalizeChannel(channel);

        return View(new SellingViewModel
        {
            Shell = shell,
            Tab = normalizedTab,
            Channel = normalizedChannel,
            Listings = []
        });
    }

    public Task<IActionResult> Summary() =>
        PageAsync("summary", "Summary", "An overall view of your account.");

    public Task<IActionResult> Accounting() =>
        PageAsync("accounting", "Accounting", "View balance, summaries, and transactions.");

    public Task<IActionResult> Submissions() =>
        PageAsync("submissions", "Submissions", "Review submitted items and progress.");

    public Task<IActionResult> Preferences() =>
        PageAsync("preferences", "Preferences", "Manage your account settings and notifications.");

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
            WalletBalance = 0m,
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
