using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    public IActionResult Detail(int id)
    {
        var model = MockUserDetailData.GetUserDetail(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }
}
