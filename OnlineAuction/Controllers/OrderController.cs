using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class OrderController : Controller
{
    private const string SessionLoggedInKey = "IsLoggedIn";
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        var model = _orderService.BuildOrderPage(HttpContext.Session);
        return View(model);
    }

    [HttpPost]
    public IActionResult PlaceBid(int auctionId, decimal amount)
    {
        if (!IsLoggedIn())
        {
            return Unauthorized(new { success = false, message = "Please sign in to place a bid." });
        }

        var result = _orderService.PlaceBid(HttpContext.Session, auctionId, amount);

        if (!result.Success)
        {
            return result.Message == "Auction not found."
                ? NotFound(new { success = false, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            redirectUrl = Url.Action(nameof(Index))
        });
    }

    [HttpPost]
    public IActionResult Complete(string paymentMethod)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

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

    private bool IsLoggedIn() =>
        string.Equals(HttpContext.Session.GetString(SessionLoggedInKey), "true", StringComparison.Ordinal);
}
