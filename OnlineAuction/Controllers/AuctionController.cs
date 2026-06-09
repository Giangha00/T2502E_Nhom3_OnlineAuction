using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class AuctionController : Controller
{
    public IActionResult Index()
    {
        var model = new AuctionViewModel
        {
            Categories = MockAuctionData.GetCategories(),
            Auctions = MockAuctionData.GetAllAuctions()
        };

        return View(model);
    }
}
