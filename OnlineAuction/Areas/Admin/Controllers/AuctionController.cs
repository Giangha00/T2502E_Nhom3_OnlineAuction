using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

[Area("Admin")]
public class AuctionController : Controller
{
    private readonly IAuctionService _auctionService;

    public AuctionController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    public IActionResult Index()
    {
        ViewBag.Auctions = _auctionService.GetAllAuctions();
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Edit(int id)
    {
        ViewBag.Auction = _auctionService.GetAuctionById(id);
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _auctionService.GetProductDetailAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }
}
