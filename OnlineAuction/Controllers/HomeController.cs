using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var allAuctions = MockAuctionData.GetAllAuctions();
        var endingSoon = allAuctions.Where(a => a.Status == "Ending Soon").ToList();

        var model = new HomeViewModel
        {
            HotAuctions = MockAuctionData.GetHotAuctions(),
            FeaturedAuctions = MockAuctionData.GetFeaturedAuctions(),
            EndingSoonAuctions = endingSoon.Skip(1).Take(3).ToList(),
            FeaturedEndingSoon = endingSoon.FirstOrDefault(),
            WonAuctions = MockAuctionData.GetWonAuctions(),
            BestSellers = MockAuctionData.GetBestSellers(),
            Categories = MockAuctionData.GetCategories(),
            VaultPosts = MockAuctionData.GetVaultPosts(),
            TotalLiveAuctions = allAuctions.Count,
            EndingSoonCount = endingSoon.Count
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
