using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index()
    {
        var model = _orderService.BuildOrderPage(HttpContext.Session);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Complete(string paymentMethod)
    {
        var result = _orderService.CompleteOrder(HttpContext.Session, paymentMethod);
        if (!result.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction("Confirmation", "Payment", new
        {
            orderRef = result.OrderRef,
            auctionName = result.AuctionName,
            total = result.Total,
            method = result.Method
        });
    }

    protected int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
