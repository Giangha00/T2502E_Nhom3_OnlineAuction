using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await _userService.GetPublicProfileAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }
}
