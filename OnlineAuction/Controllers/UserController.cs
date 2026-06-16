using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await _userService.GetPublicProfileAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        var currentUserIdText = _userManager.GetUserId(User);
        model.IsOwner = int.TryParse(currentUserIdText, out var currentUserId) && currentUserId == id;
        model.Profile.IsOwner = model.IsOwner;

        return View(model);
    }
}
