using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Products;

namespace OnlineAuction.Areas.Admin.Controllers;

public class ProductController : BaseAdminController
{
    private readonly IAdminProductService _productService;

    public ProductController(IAdminProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(ProductFilterViewModel filter)
    {
        var model = await _productService.GetProductsAsync(filter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await _productService.BuildCreateFormAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _productService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _productService.CreateAsync(model, GetCurrentUserId());

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await _productService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _productService.GetEditFormAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _productService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        var result = await _productService.UpdateAsync(model, GetCurrentUserId());

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var editModel = await _productService.GetEditFormAsync(model.Id ?? 0);
            if (editModel is not null)
            {
                model.CanChangeSeller = editModel.CanChangeSeller;
                model.ExistingGalleryImages = editModel.ExistingGalleryImages;
                model.ExistingDocuments = editModel.ExistingDocuments;
                model.PrimaryImageUrl = editModel.PrimaryImageUrl;
            }

            await _productService.PopulateFormOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _productService.GetDetailsAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id, GetCurrentUserId());

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

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
