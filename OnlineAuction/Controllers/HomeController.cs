using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuctionService _auctionService;

    public HomeController(ILogger<HomeController> logger, IAuctionService auctionService)
    {
        _logger = logger;
        _auctionService = auctionService;
    }

    public IActionResult Index()
    {
        var model = _auctionService.GetHomePage();
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
