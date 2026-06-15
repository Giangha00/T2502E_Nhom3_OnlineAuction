using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class SellController : Controller
{
    private readonly ISellService _sellService;

    public SellController(ISellService sellService)
    {
        _sellService = sellService;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(_sellService.BuildCreateForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateAuctionViewModel model)
    {
        foreach (var (key, message) in _sellService.ValidateCreateAuction(model))
        {
            ModelState.AddModelError(key, message);
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return BadRequest(ModelState);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = $"Your auction \"{model.ProductName}\" has been created successfully!";

        if (Request.Headers.ContainsKey("X-Requested-With"))
        {
            return Ok(new { success = true, message = TempData["SuccessMessage"] });
        }

        return RedirectToAction(nameof(Create));
    }
}
