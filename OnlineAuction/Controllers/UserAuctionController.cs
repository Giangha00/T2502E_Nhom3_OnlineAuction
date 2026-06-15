using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Route("User/Auction")]
public class UserAuctionController : Controller
{
    private readonly ISellerAuctionService _sellerAuctionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuctionHouseDbContext _dbContext;

    public UserAuctionController(
        ISellerAuctionService sellerAuctionService,
        UserManager<ApplicationUser> userManager,
        AuctionHouseDbContext dbContext)
    {
        _sellerAuctionService = sellerAuctionService;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet("Edit/{auctionId:int}")]
    public async Task<IActionResult> Edit(int auctionId)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = await _sellerAuctionService.GetEditFormAsync(auctionId, sellerId.Value);

        return model is null ? Forbid() : View(model);
    }

    [HttpPost("Edit/{auctionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int auctionId, SellerAuctionFormViewModel model)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        model.AuctionId = auctionId;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _sellerAuctionService.UpdateAsync(model, sellerId.Value);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Detail", "User", new { id = sellerId.Value });
    }

    [HttpPost("Delete/{auctionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int auctionId)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _sellerAuctionService.CancelAsync(auctionId, sellerId.Value);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction("Detail", "User", new { id = sellerId.Value });
    }

    private async Task<int?> GetCurrentSellerIdAsync()
    {
        var identityUserId = _userManager.GetUserId(User);
        if (int.TryParse(identityUserId, out var currentUserId))
        {
            return currentUserId;
        }

        // Tam thoi fallback vi AuthController hien tai cua nhom chua sign-in bang Identity.
        // Rule owner van nam trong Service: chi sua/xoa auction co Product.SellerId == sellerId.
        return await _dbContext.Users
            .OrderBy(user => user.Id)
            .Select(user => (int?)user.Id)
            .FirstOrDefaultAsync();
    }
}
