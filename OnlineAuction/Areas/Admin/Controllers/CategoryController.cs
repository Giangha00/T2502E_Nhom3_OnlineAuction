using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.ViewModels.Categories;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Controllers;

public class CategoryController : BaseAdminController
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Index(CategoryFilterViewModel filter)
    {
        var model = await _categoryService.GetCategoriesAsync(filter);
        return ListOrDefaultView(model, "_CategoryList");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public IActionResult Create()
    {
        return View(_categoryService.BuildCreateForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.CreateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _categoryService.GetEditFormAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var editModel = await _categoryService.GetEditFormAsync(model.Id ?? 0);
            if (editModel is not null)
            {
                model.ProductCount = editModel.ProductCount;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _categoryService.GetDetailsAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);

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
