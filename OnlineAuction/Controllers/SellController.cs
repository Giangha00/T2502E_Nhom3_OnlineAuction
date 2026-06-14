using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class SellController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View(BuildForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateAuctionViewModel model)
    {
        PopulateOptions(model);

        if (model.StartDate < DateTime.Now.AddMinutes(-1))
        {
            ModelState.AddModelError(nameof(model.StartDate), "Start date cannot be in the past.");
        }

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be greater than start date.");
        }

        if (model.BuyNowPrice.HasValue && model.BuyNowPrice.Value <= model.StartingPrice)
        {
            ModelState.AddModelError(nameof(model.BuyNowPrice), "Buy now price must be greater than starting price.");
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

    private static CreateAuctionViewModel BuildForm()
    {
        var model = new CreateAuctionViewModel
        {
            StartDate = DateTime.Now.AddHours(1),
            EndDate = DateTime.Now.AddDays(7),
            BidStep = 50,
            AuctionType = "Normal",
            Condition = "New"
        };
        PopulateOptions(model);
        return model;
    }

    private static void PopulateOptions(CreateAuctionViewModel model)
    {
        model.Categories = MockAuctionData.GetCategoryNames().ToList();
        model.Conditions = CreateAuctionMockData.Conditions.ToList();
    }
}
