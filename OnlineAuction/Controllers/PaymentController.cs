using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public IActionResult Index()
    {
        var model = _paymentService.GetPaymentInformation();
        return View(model);
    }

    public IActionResult Checkout(int? auctionId)
    {
        var model = _paymentService.BuildCheckout(auctionId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    public IActionResult Confirmation(string? orderRef, string? auctionName, decimal? total, string? method)
    {
        var model = _paymentService.BuildConfirmation(orderRef, auctionName, total, method);
        return View(model);
    }
}
