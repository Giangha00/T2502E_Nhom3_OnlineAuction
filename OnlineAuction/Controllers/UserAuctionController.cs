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
    private readonly ISellService _sellService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAuctionController(
        AuctionHouseDbContext db,
        ISellerAuctionService sellerAuctionService,
        ISellService sellService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _sellerAuctionService = sellerAuctionService;
        _sellService = sellService;
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
        model.PrimaryImageFile ??= Request.Form.Files
            .FirstOrDefault(file => file.Name == "PrimaryImageFile");
        model.GalleryImageFiles = Request.Form.Files
            .Where(file => file.Name == "GalleryImageFiles")
            .ToList();
        model.DocumentFiles = Request.Form.Files
            .Where(file => file.Name == "DocumentFiles")
            .ToList();
        model.DocumentNames = Request.Form["DocumentNames"]
            .Select(name => name!)
            .ToList();
        model.RemovedGalleryImageIds = Request.Form["RemovedGalleryImageIds"]
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
        model.RemovedDocumentIds = Request.Form["RemovedDocumentIds"]
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue)
        {
            return RedirectToLoginOrJson();
        }

        model.AuctionId = auctionId;

        if (!await IsAuctionOwnerAsync(auctionId, sellerId.Value))
        {
            return Forbid();
        }

        foreach (var (key, message) in _sellService.ValidateCreateAuction(model))
        {
            if (key == nameof(model.RegistrationStartDate))
            {
                // Edit allows keeping an already-started registration window.
                continue;
            }

            ModelState.AddModelError(key, message);
        }

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Live end must be greater than live start.");
        }

        if (!ModelState.IsValid)
        {
            await RepopulateEditFormAsync(model, auctionId, sellerId.Value);
            return EditFailure(model);
        }

        var result = await _sellerAuctionService.UpdateAsync(model, sellerId.Value);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateEditFormAsync(model, auctionId, sellerId.Value);
            return EditFailure(model, result.Message);
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

        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                redirectUrl = Url.Action("Detail", "User", new { id = sellerId.Value }) + "#seller-auctions"
            });
        }

        TempData["SuccessMessage"] = result.Message;
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

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return await RedirectToProfileAsync(sellerId.Value, auctionId);
    }

    private async Task RepopulateEditFormAsync(SellerAuctionFormViewModel model, int auctionId, int sellerId)
    {
        var fresh = await _sellerAuctionService.GetEditFormAsync(auctionId, sellerId);
        if (fresh is null)
        {
            _sellService.PopulateOptions(model);
            return;
        }

        model.Status = fresh.Status;
        model.HasBids = fresh.HasBids;
        model.CanEditFull = fresh.CanEditFull;
        model.LockRegistrationDates = fresh.LockRegistrationDates;
        model.LockLiveStartDate = fresh.LockLiveStartDate;
        model.LockStartingPrice = fresh.LockStartingPrice;
        model.LockBidStep = fresh.LockBidStep;
        model.ExistingPrimaryImage = fresh.ExistingPrimaryImage;
        model.ExistingGalleryImages = fresh.ExistingGalleryImages;
        model.ExistingDocuments = fresh.ExistingDocuments;
        model.Categories = fresh.Categories;
        model.Authenticators = fresh.Authenticators;
        model.GradeValues = fresh.GradeValues;
        model.Languages = fresh.Languages;
    }

    private IActionResult EditFailure(SellerAuctionFormViewModel model, string? message = null)
    {
        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return BadRequest(new
            {
                success = false,
                message = message
                    ?? ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => error.ErrorMessage)
                        .FirstOrDefault()
                    ?? "Please check the auction form."
            });
        }

        return View(model);
    }

    private IActionResult RedirectToLoginOrJson()
    {
        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Unauthorized(new { success = false, message = "Please sign in." });
        }

        return RedirectToAction("Login", "Auth");
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
