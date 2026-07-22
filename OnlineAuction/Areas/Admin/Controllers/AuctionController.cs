using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Auctions;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.Controllers;

public class AuctionController : BaseAdminController
{
    private readonly AdminAuctionService _auctionService;

    public AuctionController(AdminAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    [RequirePermission(PermissionCodes.AuctionsView)]
    public async Task<IActionResult> Index(AuctionFilterViewModel filter)
    {
        var model = await _auctionService.GetAuctionsAsync(filter);
        return ListOrDefaultView(model, "_AuctionList");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create Listing";
        return View();
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> CreateAuction()
    {
        ViewData["Title"] = "Create Auction";
        return View(await _auctionService.BuildCreateAuctionFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> CreateAuction(AuctionFormViewModel model)
    {
        model.ListingType = ListingTypes.Auction;
        BindUploadedFiles(model);
        RevalidateModel(model);

        if (!ModelState.IsValid)
        {
            await _auctionService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _auctionService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _auctionService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> CreateBuyNow()
    {
        ViewData["Title"] = "Create Buy Now";
        return View(await _auctionService.BuildCreateBuyNowFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> CreateBuyNow(AuctionFormViewModel model)
    {
        model.ListingType = ListingTypes.BuyNow;
        BindUploadedFiles(model);
        RevalidateModel(model);

        if (!ModelState.IsValid)
        {
            await _auctionService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _auctionService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _auctionService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Index", "BuyNow", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Create(AuctionFormViewModel model)
    {
        if (model.ListingType == ListingTypes.BuyNow)
        {
            return await CreateBuyNow(model);
        }

        return await CreateAuction(model);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _auctionService.GetEditFormAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Edit(AuctionFormViewModel model)
    {
        BindUploadedFiles(model);
        RevalidateModel(model);

        if (!ModelState.IsValid)
        {
            await _auctionService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _auctionService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _auctionService.PopulateFormOptionsAsync(model);
            var refreshed = await _auctionService.GetEditFormAsync(model.Id);
            if (refreshed is not null)
            {
                model.IsScheduleLocked = refreshed.IsScheduleLocked;
                model.IsStartingPriceLocked = refreshed.IsStartingPriceLocked;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return model.IsBuyNow
            ? RedirectToAction("Index", "BuyNow", new { area = "Admin" })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsView)]
    public async Task<IActionResult> Details(int id, int bidPage = 1, bool flaggedOnly = false)
    {
        var model = await _auctionService.GetDetailsAsync(id, bidPage, flaggedOnly);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> ReviewFraudAlert(long alertId, int auctionId)
    {
        var result = await _auctionService.ReviewFraudAlertAsync(
            alertId,
            GetCurrentAdminId(),
            FraudAlertStatuses.Reviewed);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = auctionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> DismissFraudAlert(long alertId, int auctionId)
    {
        var result = await _auctionService.ReviewFraudAlertAsync(
            alertId,
            GetCurrentAdminId(),
            FraudAlertStatuses.Dismissed);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = auctionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _auctionService.CancelAsync(id, GetCurrentAdminId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _auctionService.DeleteAsync(id, GetCurrentAdminId());

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private void BindUploadedFiles(AuctionFormViewModel model)
    {
        model.PrimaryImageFile ??= Request.Form.Files
            .FirstOrDefault(file => file.Name == nameof(AuctionFormViewModel.PrimaryImageFile));

        var galleryFiles = Request.Form.Files
            .Where(file => file.Name == "GalleryImageFiles")
            .ToList();
        if (galleryFiles.Count > 0)
        {
            model.GalleryImageFiles = galleryFiles;
        }

        var documentFiles = Request.Form.Files
            .Where(file => file.Name == "DocumentFiles")
            .ToList();
        if (documentFiles.Count > 0)
        {
            model.DocumentFiles = documentFiles;
        }

        var documentNames = Request.Form["DocumentNames"]
            .Select(name => name!)
            .ToList();
        if (documentNames.Count > 0)
        {
            model.DocumentNames = documentNames;
        }
        else if (model.DocumentFiles.Count > 0)
        {
            model.DocumentNames = model.DocumentFiles
                .Select(file => file.FileName)
                .ToList();
        }
    }

    private void RevalidateModel(AuctionFormViewModel model)
    {
        model.NormalizeGrading();
        ModelState.Clear();
        TryValidateModel(model, prefix: string.Empty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> BulkDelete(AuctionBulkDeleteViewModel model)
    {
        var result = await _auctionService.BulkDeleteAsync(model.SelectedAuctionIds, GetCurrentAdminId());

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
