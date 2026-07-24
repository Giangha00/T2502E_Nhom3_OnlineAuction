using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.BuyNow;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;

namespace OnlineAuction.Areas.Admin.Controllers;

/// <summary>
/// Admin Buy Now management. Permissions reuse auctions.view / auctions.manage
/// (no separate buynow.* codes — same operators manage both listing types).
/// </summary>
public class BuyNowController : BaseAdminController
{
    private readonly IAdminBuyNowService _buyNowService;
    private readonly AdminAuctionService _auctionService;

    public BuyNowController(IAdminBuyNowService buyNowService, AdminAuctionService auctionService)
    {
        _buyNowService = buyNowService;
        _auctionService = auctionService;
    }

    [RequirePermission(PermissionCodes.AuctionsView)]
    public async Task<IActionResult> Index(BuyNowFilterViewModel filter)
    {
        var model = await _buyNowService.GetListingsAsync(filter);
        model.CanManage = AdminPermissionHelper.Can(User, PermissionCodes.AuctionsManage);
        return ListOrDefaultView(model, "_BuyNowList");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsView)]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _buyNowService.GetDetailsAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        model.CanManage = AdminPermissionHelper.Can(User, PermissionCodes.AuctionsManage);
        return View(model);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Create()
    {
        return View(await _buyNowService.BuildCreateFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Create(BuyNowFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _buyNowService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _buyNowService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _buyNowService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _buyNowService.GetEditFormAsync(id);

        if (model is null)
        {
            TempData["ErrorMessage"] = "This Buy Now listing cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Edit(BuyNowFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _buyNowService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _buyNowService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _buyNowService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _buyNowService.CancelAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> BulkDelete(BuyNowBulkDeleteViewModel model)
    {
        var result = await _auctionService.BulkDeleteAsync(model.SelectedBuyNowIds, GetCurrentAdminId());

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
}
