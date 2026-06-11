using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    public IActionResult Detail(int id)
    {
        var seller = MockAuctionData.GetBestSellers().FirstOrDefault(s => s.Id == id);
        if (seller is null)
        {
            return NotFound();
        }

        var model = new UserDetailViewModel
        {
            Seller = seller,
            ActiveListings = MockAuctionData.GetAuctionsBySellerId(id)
        };

        return View(model);
    }
}
