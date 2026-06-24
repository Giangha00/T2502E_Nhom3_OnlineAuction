using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class BuyNowController : Controller
{
    private readonly IAuctionService _auctionService;

    public BuyNowController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _auctionService.GetAuctionIndexAsync(ListingTypes.BuyNow);
        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _auctionService.GetProductDetailAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId)
    {
        var product = await _auctionService.GetProductDetailAsync(productId);
        if (product is null)
        {
            return NotFound(new { success = false, message = "Product not found." });
        }

        var imageUrl = product.Images.FirstOrDefault() ?? string.Empty;
        CartStore.AddItem(HttpContext.Session, new CartItemViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            ImageUrl = imageUrl,
            Price = product.CurrentPrice,
            Category = product.Category
        });

        return Json(new
        {
            success = true,
            message = "Added to cart.",
            cartCount = CartStore.GetCount(HttpContext.Session)
        });
    }
}
