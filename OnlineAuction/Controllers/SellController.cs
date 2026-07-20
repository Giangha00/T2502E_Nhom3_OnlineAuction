using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.User)]
public class SellController : Controller
{
    private readonly ISellService _sellService;
    private readonly ISellerAuctionService _sellerAuctionService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public SellController(
        ISellService sellService,
        ISellerAuctionService sellerAuctionService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        UserManager<ApplicationUser> userManager)
    {
        _sellService = sellService;
        _sellerAuctionService = sellerAuctionService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
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

        foreach (var (key, message) in _sellService.ValidateCreateAuction(model))
        {
            ModelState.AddModelError(key, message);
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => error.ErrorMessage)
                        .FirstOrDefault() ?? "Please check the auction form."
                });
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

        await NotifyListingAsync(
            sellerId.Value,
            _notifyLocalizer[NotificationKeys.ListingSubmittedTitle],
            _notifyLocalizer[NotificationKeys.ListingSubmittedMessage],
            NotificationReferenceTypes.ListingSubmitted,
            result.AuctionId);

        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                redirectUrl = Url.Action("Detail", "User", new { id = sellerId.Value }) + "#seller-auctions"
            });
        }

        return RedirectToAction("Selling", "Account", new { tab = "active", channel = "auction" });
    }

    [HttpGet]
    public IActionResult BuyNow()
    {
        return View(_sellService.BuildBuyNowForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(CreateBuyNowViewModel model)
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

        foreach (var (key, message) in _sellService.ValidateCreateBuyNow(model))
        {
            ModelState.AddModelError(key, message);
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => error.ErrorMessage)
                        .FirstOrDefault() ?? "Please check the listing form."
                });
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

            return View(model);
        }

        var result = await _sellerAuctionService.CreateBuyNowAsync(model, sellerId.Value);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return View(model);
        }

        await NotifyListingAsync(
            sellerId.Value,
            _notifyLocalizer[NotificationKeys.ListingSubmittedTitle],
            _notifyLocalizer[NotificationKeys.ListingSubmittedMessage],
            NotificationReferenceTypes.ListingSubmitted,
            result.AuctionId);

        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                redirectUrl = Url.Action("Detail", "User", new { id = sellerId.Value }) + "#seller-buynow"
            });
        }

        return RedirectToAction("Detail", "User", new { id = sellerId.Value }, fragment: "seller-buynow");
    }

    private async Task NotifyListingAsync(
        int userId,
        string title,
        string message,
        string referenceType,
        int? auctionId)
    {
        await _notificationService.CreateAndPushAsync(
            userId,
            title,
            message,
            NotificationType.Auction,
            auctionId is > 0 ? $"/Auction/Detail/{auctionId.Value}" : "/User/Detail/" + userId,
            referenceType,
            auctionId,
            debounceWindow: TimeSpan.FromMinutes(2));
    }

    private async Task<int?> GetCurrentSellerIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id;
    }
}
