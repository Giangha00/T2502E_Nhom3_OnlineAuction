using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public CategoryController(
        AppDbContext context,
        ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    public IActionResult Index(string search, int page = 1)
    {
        int pageSize = 5;

        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var totalItems = query.Count();

        var categories = query
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages =
            (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.Search = search;

        return View(categories);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Category category,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        if (imageFile != null)
        {
            category.ImageUrl =
                await _cloudinaryService
                    .UploadImageAsync(imageFile);
        }

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var category =
            _context.Categories.Find(id);

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        Category category,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        var existingCategory =
            await _context.Categories.FindAsync(category.Id);

        if (existingCategory == null)
        {
            return NotFound();
        }

        existingCategory.Name = category.Name;
        existingCategory.Description = category.Description;

        if (imageFile != null)
        {
            existingCategory.ImageUrl =
                await _cloudinaryService
                    .UploadImageAsync(imageFile);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var category =
            _context.Categories.Find(id);

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        var category =
            _context.Categories.Find(id);

        if (category != null)
        {
            _context.Categories.Remove(category);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
}