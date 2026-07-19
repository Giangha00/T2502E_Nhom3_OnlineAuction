using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
[Route("User/Auction")]
public class UserAuctionController : Controller
{
    private readonly AuctionHouseDbContext _db;
    private readonly ISellerAuctionService _sellerAuctionService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAuctionController(
        AuctionHouseDbContext db,
        ISellerAuctionService sellerAuctionService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _sellerAuctionService = sellerAuctionService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _userManager = userManager;
    }

    [HttpGet("Edit/{auctionId:int}")]
    [Authorize(Policy = "ListingOwner")]
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
    [Authorize(Policy = "ListingOwner")]
    public async Task<IActionResult> Edit(int auctionId, SellerAuctionFormViewModel model)
    {
        model.PrimaryImageFile ??= Request.Form.Files.FirstOrDefault();

        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        model.AuctionId = auctionId;

        if (!await IsAuctionOwnerAsync(auctionId, sellerId.Value))
        {
            return Forbid();
        }

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be greater than start date.");
        }

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

        await _notificationService.CreateAndPushAsync(
            sellerId.Value,
            _notifyLocalizer[NotificationKeys.ListingUpdatedTitle],
            _notifyLocalizer[NotificationKeys.ListingUpdatedMessage],
            NotificationType.Auction,
            $"/Auction/Detail/{auctionId}",
            NotificationReferenceTypes.ListingUpdated,
            auctionId,
            debounceWindow: TimeSpan.FromMinutes(2));

        return await RedirectToProfileAsync(sellerId.Value, auctionId);
    }

    [HttpPost("Delete/{auctionId:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ListingOwner")]
    public async Task<IActionResult> Delete(int auctionId)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!await IsAuctionOwnerAsync(auctionId, sellerId.Value))
        {
            return Forbid();
        }

        var result = await _sellerAuctionService.CancelAsync(auctionId, sellerId.Value);

        await _notificationService.CreateAndPushAsync(
            sellerId.Value,
            result.Success
                ? _notifyLocalizer[NotificationKeys.ListingCancelledTitle]
                : _notifyLocalizer[NotificationKeys.ListingCancelFailedTitle],
            result.Success ? _notifyLocalizer[NotificationKeys.ListingCancelledMessage] : result.Message,
            result.Success ? NotificationType.Auction : NotificationType.System,
            $"/User/Detail/{sellerId.Value}",
            NotificationReferenceTypes.ListingCancelled,
            auctionId,
            debounceWindow: TimeSpan.FromMinutes(2));

        return await RedirectToProfileAsync(sellerId.Value, auctionId);
    }

    private async Task<IActionResult> RedirectToProfileAsync(int sellerId, int auctionId)
    {
        var listingType = await _db.Auctions.AsNoTracking()
            .Where(auction => auction.Id == auctionId)
            .Select(auction => auction.ListingType)
            .FirstOrDefaultAsync();

        var fragment = string.Equals(listingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase)
            ? "seller-buynow"
            : "seller-auctions";

        return RedirectToAction("Detail", "User", new { id = sellerId }, fragment);
    }

    private async Task<int?> GetCurrentSellerIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id;
    }

    private Task<bool> IsAuctionOwnerAsync(int auctionId, int sellerId)
    {
        return _db.Auctions.AnyAsync(auction =>
            auction.Id == auctionId &&
            auction.Product.SellerId == sellerId);
    }
}
