using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Products;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class ProductController : BaseAdminController
{
    private readonly IAdminProductService _productService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IProductDocumentDownloadService _documentDownloadService;

    public ProductController(
        IAdminProductService productService,
        ICurrentUserContext currentUserContext,
        IProductDocumentDownloadService documentDownloadService)
    {
        _productService = productService;
        _currentUserContext = currentUserContext;
        _documentDownloadService = documentDownloadService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ProductCategoryFilterViewModel filter)
    {
        var model = await _productService.GetCategoryTemplatesAsync(filter);
        return ListOrDefaultView(model, "_ProductCategoryList");
    }

    [HttpGet]
    public async Task<IActionResult> Category(int id, ProductFilterViewModel filter)
    {
        var model = await _productService.GetCategoryProductsAsync(id, filter);

        if (model is null)
        {
            return NotFound();
        }

        return ListOrDefaultView(model, "_ProductList");
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? categoryId = null)
    {
        var model = await _productService.BuildCreateFormAsync();

        if (categoryId.HasValue)
        {
            model.CategoryId = categoryId.Value;
            await RepopulateOptionsAsync(model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _productService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _productService.BuildEditFormAsync(id);

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
            await RepopulateOptionsAsync(model);
            var editModel = await _productService.BuildEditFormAsync(model.Id ?? 0);
            if (editModel is not null)
            {
                model.IsSellerLocked = editModel.IsSellerLocked;
                model.ExistingGalleryImages = editModel.ExistingGalleryImages;
                model.ExistingDocuments = editModel.ExistingDocuments;
                model.PrimaryImageUrl = editModel.PrimaryImageUrl;
            }

            return View(model);
        }

        var result = await _productService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateOptionsAsync(model);
            var editModel = await _productService.BuildEditFormAsync(model.Id ?? 0);
            if (editModel is not null)
            {
                model.IsSellerLocked = editModel.IsSellerLocked;
                model.ExistingGalleryImages = editModel.ExistingGalleryImages;
                model.ExistingDocuments = editModel.ExistingDocuments;
                model.PrimaryImageUrl = editModel.PrimaryImageUrl;
            }

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

    [HttpGet]
    [RequirePermission(PermissionCodes.ProductsManage)]
    public async Task<IActionResult> DownloadDocument(int id, CancellationToken cancellationToken)
    {
        var result = await _documentDownloadService.GetDownloadAsync(id, isAdminRequest: true, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Status switch
        {
            ProductDocumentDownloadStatus.NotFound => NotFound(),
            ProductDocumentDownloadStatus.Success => Redirect(result.FileUrl),
            _ => NotFound()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? returnCategoryId)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync();
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _productService.DeleteAsync(id, adminId.Value);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectAfterMutation(returnCategoryId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(ProductBulkDeleteViewModel model)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync();
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _productService.BulkDeleteAsync(model.SelectedProductIds, adminId.Value);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectAfterMutation(model.ReturnCategoryId);
    }

    private IActionResult RedirectAfterMutation(int? returnCategoryId)
    {
        if (returnCategoryId.HasValue)
        {
            return RedirectToAction(nameof(Category), new { id = returnCategoryId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task RepopulateOptionsAsync(ProductFormViewModel model)
    {
        ProductFormViewModel fresh;
        if (model.Id.HasValue)
        {
            fresh = await _productService.BuildEditFormAsync(model.Id.Value)
                    ?? await _productService.BuildCreateFormAsync();
        }
        else
        {
            fresh = await _productService.BuildCreateFormAsync();
        }

        model.CategoryOptions = fresh.CategoryOptions;
        model.SellerOptions = fresh.SellerOptions;
        model.ConditionOptions = fresh.ConditionOptions;
        model.GradeOptions = fresh.GradeOptions;
        model.LanguageOptions = fresh.LanguageOptions;
    }
}
