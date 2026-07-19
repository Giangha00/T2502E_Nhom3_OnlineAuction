using System.IO;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Hosting;
namespace OnlineAuction.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly IHostEnvironment _env;

    public PaymentController(
        IPaymentService paymentService,
        IOrderPaymentService orderPaymentService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        IHostEnvironment env)
    {
        _paymentService = paymentService;
        _orderPaymentService = orderPaymentService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _env = env;
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
   [HttpPost]
// Cho phép nhận request POST từ PayPal.
// PayPal sẽ gửi dữ liệu bằng HTTP POST tới endpoint này.

[IgnoreAntiforgeryToken]
// Vì PayPal không có AntiForgeryToken của website mình.
// Nếu không bỏ qua, ASP.NET sẽ chặn request từ PayPal.

public async Task<IActionResult> PayPalIpn()
{
    /*
     * IPN = Instant Payment Notification
     * Đây là endpoint để PayPal gọi ngược về server của mình.
     *
     * URL ví dụ:
     * POST /Payment/PayPalIpn
     */

    // Đọc toàn bộ dữ liệu PayPal gửi lên dưới dạng form
    var form = await Request.ReadFormAsync();

    /*
     * Ví dụ dữ liệu PayPal gửi:
     *
     * payment_status=Completed
     * txn_id=TEST001
     * paypal_order_id=ABC123
     * mc_gross=20.00
     */

    // Disable this test IPN endpoint in non-development environments to avoid
    // spoofing and accidental production writes.
    if (!_env.IsDevelopment())
    {
        return NotFound();
    }

    // Trạng thái thanh toán
    // Completed = thành công
    // Pending = đang chờ
    // Refunded = hoàn tiền
    var paymentStatus = form["payment_status"].ToString();

    // Mã giao dịch PayPal
    // Dùng để chống xử lý trùng
    var transactionId = form["txn_id"].ToString();

    // Mã PayPal Order
    // Dùng để tìm Payment tương ứng trong database
    var payPalOrderId = form["paypal_order_id"].ToString();

    // Số tiền thanh toán
    var amount = form["mc_gross"].ToString();

    /*
     * In ra Console để dễ debug.
     * Khi test sẽ nhìn thấy dữ liệu PayPal gửi về.
     */

    Console.WriteLine("===== PAYPAL IPN RECEIVED =====");

    Console.WriteLine($"payment_status = {paymentStatus}");

    Console.WriteLine($"txn_id = {transactionId}");

    Console.WriteLine($"paypal_order_id = {payPalOrderId}");

    Console.WriteLine($"mc_gross = {amount}");

    /*
     * Gọi Service xử lý nghiệp vụ.
     *
     * Controller chỉ nên:
     * - Nhận request
     * - Trả response
     *
     * Không nên xử lý database ở đây.
     */

    var result = await _orderPaymentService.TestProcessIpnAsync(
        payPalOrderId,
        transactionId,
        paymentStatus
    );

    /*
     * Trả kết quả về cho PayPal.
     *
     * Trong production thường chỉ cần:
     *
     * return Ok();
     *
     * Vì PayPal chỉ quan tâm HTTP 200.
     */

    return Ok(result);
}

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PayPalWebhook()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return BadRequest("Empty PayPal webhook payload.");
        }

        var result = await _orderPaymentService.ProcessPayPalWebhookAsync(requestBody, Request.Headers);
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Unable to process PayPal webhook.");
        }

        return Ok();
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    public async Task<IActionResult> Confirmation(int orderId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = await _orderPaymentService.GetPaidOrderConfirmationAsync(userId.Value, orderId);
        if (model is null)
        {
            return RedirectToAction("Index", "Order");
        }

        return View(model);
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    public async Task<IActionResult> PayPalReturn(string? token)
    { 
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            await NotifyPaymentIssueAsync(
                userId.Value,
                _notifyLocalizer[NotificationKeys.PaymentMissingTokenMessage]);
            return RedirectToAction("Index", "Order");
        }

        var captureResult = await _orderPaymentService.CapturePayPalCheckoutAsync(userId.Value, token);
        if (!captureResult.Success)
        {
            await NotifyPaymentIssueAsync(
                userId.Value,
                captureResult.ErrorMessage ?? _notifyLocalizer[NotificationKeys.PaymentCouldNotCompleteMessage]);
            return RedirectToAction("Index", "Order");
        }

        return RedirectToAction(nameof(Confirmation), new { orderId = captureResult.PrimaryOrderId });
    }
    

    [Authorize(AuthenticationSchemes = AuthSchemes.User)]
    public async Task<IActionResult> PayPalCancel(string? token)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await _orderPaymentService.CancelPayPalCheckoutAsync(userId.Value, token);
            await NotifyPaymentIssueAsync(
                userId.Value,
                _notifyLocalizer[NotificationKeys.PaymentCancelledMessage],
                NotificationReferenceTypes.PaymentCancelled);
        }

        return RedirectToAction("Index", "Order");
    }

    private async Task NotifyPaymentIssueAsync(
        int userId,
        string message,
        string referenceType = NotificationReferenceTypes.PaymentFailed)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await _notificationService.CreateAndPushAsync(
            userId,
            referenceType == NotificationReferenceTypes.PaymentCancelled
                ? _notifyLocalizer[NotificationKeys.PaymentCancelledTitle]
                : _notifyLocalizer[NotificationKeys.PaymentFailedTitle],
            message,
            NotificationType.Payment,
            "/Order",
            referenceType,
            userId,
            debounceWindow: TimeSpan.FromMinutes(2));
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
    
}
