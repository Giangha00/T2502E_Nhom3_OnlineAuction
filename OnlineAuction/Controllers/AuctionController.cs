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

    public IActionResult Detail(int id)
    {
        var product = MockProductDetailData.GetById(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }
}
