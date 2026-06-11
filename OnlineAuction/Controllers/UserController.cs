using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;

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

        return View(seller);
    }
}
