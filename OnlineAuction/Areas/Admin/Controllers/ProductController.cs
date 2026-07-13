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
    public async Task<IActionResult> Index(ProductTemplateFilterViewModel filter)
    {
        var model = await _productService.GetProductTemplatesAsync(filter);
        return ListOrDefaultView(model, "_ProductTemplateList");
    }

    [HttpGet]
    public async Task<IActionResult> Template(int id, ProductFilterViewModel filter)
    {
        var model = await _productService.GetTemplateInstancesAsync(id, filter);

        if (model is null)
        {
            return NotFound();
        }

        return ListOrDefaultView(model, "_ProductList");
    }

    [HttpGet]
    public async Task<IActionResult> CreateTemplate()
    {
        var model = await _productService.BuildCreateTemplateFormAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(ProductTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateTemplateOptionsAsync(model);
            return View(model);
        }

        var result = await _productService.CreateTemplateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateTemplateOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditTemplate(int id)
    {
        var model = await _productService.BuildEditTemplateFormAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTemplate(ProductTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateTemplateOptionsAsync(model);
            return View(model);
        }

        var result = await _productService.UpdateTemplateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateTemplateOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var adminId = await _currentUserContext.GetAdminIdAsync();
        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var result = await _productService.DeleteTemplateAsync(id, adminId.Value);

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

    [HttpGet]
    public async Task<IActionResult> Create(int? templateId = null)
    {
        var model = await _productService.BuildCreateFormAsync(templateId);

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
        return RedirectAfterMutation(model.ContextTemplateId ?? model.ProductTemplateId);
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
        return RedirectAfterMutation(model.ContextTemplateId ?? model.ProductTemplateId);
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
    public async Task<IActionResult> Delete(int id, int? returnTemplateId, int? returnCategoryId)
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

        return RedirectAfterMutation(returnTemplateId);
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

        return RedirectAfterMutation(model.ReturnTemplateId);
    }

    private IActionResult RedirectAfterMutation(int? returnTemplateId)
    {
        if (returnTemplateId.HasValue)
        {
            return RedirectToAction(nameof(Template), new { id = returnTemplateId.Value });
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
        model.ProductTemplateOptions = fresh.ProductTemplateOptions;
    }

    private async Task RepopulateTemplateOptionsAsync(ProductTemplateFormViewModel model)
    {
        ProductTemplateFormViewModel fresh;
        if (model.Id.HasValue)
        {
            fresh = await _productService.BuildEditTemplateFormAsync(model.Id.Value)
                    ?? await _productService.BuildCreateTemplateFormAsync();
        }
        else
        {
            fresh = await _productService.BuildCreateTemplateFormAsync();
        }

        model.CategoryOptions = fresh.CategoryOptions;
        model.GradeOptions = fresh.GradeOptions;
        model.LanguageOptions = fresh.LanguageOptions;
    }
}
