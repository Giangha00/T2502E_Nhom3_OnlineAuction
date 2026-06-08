using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        var model = new HomeViewModel
        {
            FeaturedAuctions =
            [
                new AuctionItemViewModel
                {
                    Id = 1,
                    Name = "Abstract Expression No. 7",
                    Category = "Fine Art",
                    ImageUrl = "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=600&h=750&fit=crop",
                    StartingPrice = 850,
                    CurrentPrice = 1420,
                    Status = "Live",
                    TimeRemaining = "2d 14h left"
                },
                new AuctionItemViewModel
                {
                    Id = 2,
                    Name = "Vintage Leica M3 Camera",
                    Category = "Collectibles",
                    ImageUrl = "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=600&h=750&fit=crop",
                    StartingPrice = 1200,
                    CurrentPrice = 2850,
                    Status = "Live",
                    TimeRemaining = "5h 32m left"
                },
                new AuctionItemViewModel
                {
                    Id = 3,
                    Name = "Mid-Century Walnut Chair",
                    Category = "Furniture",
                    ImageUrl = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=600&h=750&fit=crop",
                    StartingPrice = 400,
                    CurrentPrice = 675,
                    Status = "Live",
                    TimeRemaining = "1d 8h left"
                },
                new AuctionItemViewModel
                {
                    Id = 4,
                    Name = "Ceramic Vase — Kyoto Studio",
                    Category = "Decor",
                    ImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b7a?w=600&h=750&fit=crop",
                    StartingPrice = 150,
                    CurrentPrice = 310,
                    Status = "Live",
                    TimeRemaining = "18h 45m left"
                },
                new AuctionItemViewModel
                {
                    Id = 5,
                    Name = "First Edition — The Great Gatsby",
                    Category = "Books",
                    ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&h=750&fit=crop",
                    StartingPrice = 2000,
                    CurrentPrice = 4200,
                    Status = "Live",
                    TimeRemaining = "3d 2h left"
                },
                new AuctionItemViewModel
                {
                    Id = 6,
                    Name = "Swiss Automatic Watch 1962",
                    Category = "Watches",
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&h=750&fit=crop",
                    StartingPrice = 950,
                    CurrentPrice = 1780,
                    Status = "Live",
                    TimeRemaining = "6h 10m left"
                }
            ],
            BestSellers =
            [
                new SellerViewModel
                {
                    Id = 1,
                    Username = "ElenaVoss",
                    AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&h=200&fit=crop&crop=face",
                    AuctionCount = 48,
                    SuccessfulSales = 41,
                    Rating = 4.9
                },
                new SellerViewModel
                {
                    Id = 2,
                    Username = "MarcusChen",
                    AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop&crop=face",
                    AuctionCount = 36,
                    SuccessfulSales = 33,
                    Rating = 4.8
                },
                new SellerViewModel
                {
                    Id = 3,
                    Username = "SofiaArtGallery",
                    AvatarUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop&crop=face",
                    AuctionCount = 72,
                    SuccessfulSales = 68,
                    Rating = 5.0
                },
                new SellerViewModel
                {
                    Id = 4,
                    Username = "JamesRetro",
                    AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=200&h=200&fit=crop&crop=face",
                    AuctionCount = 29,
                    SuccessfulSales = 25,
                    Rating = 4.6
                }
            ]
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View("About");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
