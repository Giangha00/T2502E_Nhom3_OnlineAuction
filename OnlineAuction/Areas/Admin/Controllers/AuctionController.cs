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
    public async Task<IActionResult> Create()
    {
        return View(await _auctionService.BuildCreateFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AuctionsManage)]
    public async Task<IActionResult> Create(AuctionFormViewModel model)
    {
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
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _auctionService.DeleteAsync(id);

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
