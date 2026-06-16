using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize]
public class SellController : Controller
{
    private readonly ISellService _sellService;
    private readonly ISellerAuctionService _sellerAuctionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SellController(
        ISellService sellService,
        ISellerAuctionService sellerAuctionService,
        UserManager<ApplicationUser> userManager)
    {
        _sellService = sellService;
        _sellerAuctionService = sellerAuctionService;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(_sellService.BuildCreateForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAuctionViewModel model)
    {
        // View upload hien tai chua dat name cho input file.
        // Neu sau nay view gui file len bang bat ky input file nao, dong nay se lay file dau tien de upload Cloudinary.
        model.PrimaryImageFile ??= Request.Form.Files.FirstOrDefault();

        foreach (var (key, message) in _sellService.ValidateCreateAuction(model))
        {
            ModelState.AddModelError(key, message);
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(ModelState);
            }

            return View(model);
        }

        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            const string message = "No seller account was found. Please create a user account first.";

            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(new { success = false, message });
            }

            TempData["ErrorMessage"] = message;
            return View(model);
        }

        var result = await _sellerAuctionService.CreateAsync(model, sellerId.Value);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;

        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                redirectUrl = Url.Action("Detail", "User", new { id = sellerId.Value }) + "#seller-auctions"
            });
        }

        return RedirectToAction("Selling", "Account", new { tab = "active", channel = "buynow" });
    }

    private async Task<int?> GetCurrentSellerIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id;
    }
}
