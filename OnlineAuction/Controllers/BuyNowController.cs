using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
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

    public IActionResult Index()
    {
        var model = _auctionService.GetAuctionIndex();
        return View(model);
    }

    public IActionResult Detail(int id)
    {
        var product = _auctionService.GetProductDetail(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int productId)
    {
        var product = _auctionService.GetProductDetail(productId);
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
