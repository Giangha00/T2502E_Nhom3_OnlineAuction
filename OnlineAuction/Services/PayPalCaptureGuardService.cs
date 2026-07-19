using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PayPalCaptureGuardService : IPayPalCaptureGuardService
{
    private static readonly HashSet<string> CapturableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "APPROVED",
        "CREATED"
    };

    private readonly IPayPalService _payPalService;
    private readonly AuctionHouseDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<PayPalCaptureGuardService> _logger;

    public PayPalCaptureGuardService(
        IPayPalService payPalService,
        AuctionHouseDbContext dbContext,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        ILogger<PayPalCaptureGuardService> logger)
    {
        _payPalService = payPalService;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _logger = logger;
    }

    public async Task<SafePayPalCaptureResult> SafeCaptureAsync(
        string payPalOrderId,
        decimal expectedAmount,
        PayPalCaptureContext context,
        CancellationToken cancellationToken = default)
    {
        var orderDetails = await _payPalService.GetOrderDetailsAsync(payPalOrderId, cancellationToken);
        if (!orderDetails.Success)
        {
            LogCorrelation(
                LogLevel.Warning,
                context,
                payPalOrderId,
                expectedAmount,
                null,
                null,
                "pre_capture_order_lookup_failed",
                orderDetails.ErrorMessage);

            return SafePayPalCaptureResult.Fail(
                orderDetails.ErrorMessage ?? "Unable to verify PayPal checkout before capture.");
        }

        LogCorrelation(
            LogLevel.Information,
            context,
            payPalOrderId,
            expectedAmount,
            orderDetails.OrderAmount,
            orderDetails.Status,
            "pre_capture_validation");

        if (orderDetails.IsCaptured)
        {
            if (!PayPalAmountHelper.AmountsMatch(expectedAmount, orderDetails.CapturedAmount!.Value))
            {
                var refundResult = await TryRefundAndAlertAsync(
                    orderDetails.CaptureId!,
                    orderDetails.CapturedAmount.Value,
                    expectedAmount,
                    context,
                    payPalOrderId,
                    "already_captured_amount_mismatch",
                    cancellationToken);

                return SafePayPalCaptureResult.Fail(
                    "PayPal payment amount did not match the expected total. A refund has been initiated.",
                    refundResult.Attempted,
                    refundResult.Succeeded);
            }

            return SafePayPalCaptureResult.FromExistingCapture(
                orderDetails.CaptureId!,
                orderDetails.CapturedAmount!.Value);
        }

        if (!PayPalAmountHelper.AmountsMatch(expectedAmount, orderDetails.OrderAmount))
        {
            LogCorrelation(
                LogLevel.Warning,
                context,
                payPalOrderId,
                expectedAmount,
                orderDetails.OrderAmount,
                orderDetails.Status,
                "pre_capture_amount_mismatch");

            return SafePayPalCaptureResult.Fail(
                "PayPal order amount did not match the expected total. Capture was not attempted.");
        }

        if (!IsCapturableStatus(orderDetails.Status))
        {
            LogCorrelation(
                LogLevel.Warning,
                context,
                payPalOrderId,
                expectedAmount,
                orderDetails.OrderAmount,
                orderDetails.Status,
                "pre_capture_invalid_status");

            return SafePayPalCaptureResult.Fail(
                "PayPal checkout is not in a capturable state. Please start checkout again.");
        }

        var captureResult = await _payPalService.CaptureOrderAsync(payPalOrderId, cancellationToken);
        if (!captureResult.Success || string.IsNullOrWhiteSpace(captureResult.CaptureId))
        {
            LogCorrelation(
                LogLevel.Warning,
                context,
                payPalOrderId,
                expectedAmount,
                orderDetails.OrderAmount,
                orderDetails.Status,
                "capture_failed",
                captureResult.ErrorMessage);

            return SafePayPalCaptureResult.Fail(
                captureResult.ErrorMessage ?? "Payment capture failed. Please try again.");
        }

        if (!PayPalAmountHelper.AmountsMatch(expectedAmount, captureResult.CapturedAmount))
        {
            var refundResult = await TryRefundAndAlertAsync(
                captureResult.CaptureId,
                captureResult.CapturedAmount,
                expectedAmount,
                context,
                payPalOrderId,
                "post_capture_amount_mismatch",
                cancellationToken);

            return SafePayPalCaptureResult.Fail(
                "Payment amount did not match the expected total. A refund has been initiated.",
                refundResult.Attempted,
                refundResult.Succeeded);
        }

        LogCorrelation(
            LogLevel.Information,
            context,
            payPalOrderId,
            expectedAmount,
            captureResult.CapturedAmount,
            orderDetails.Status,
            "capture_succeeded",
            captureId: captureResult.CaptureId);

        return captureResult.AlreadyCaptured
            ? SafePayPalCaptureResult.FromExistingCapture(captureResult.CaptureId, captureResult.CapturedAmount)
            : SafePayPalCaptureResult.Ok(captureResult.CaptureId, captureResult.CapturedAmount);
    }

    private async Task<(bool Attempted, bool Succeeded)> TryRefundAndAlertAsync(
        string captureId,
        decimal capturedAmount,
        decimal expectedAmount,
        PayPalCaptureContext context,
        string payPalOrderId,
        string reason,
        CancellationToken cancellationToken)
    {
        LogCorrelation(
            LogLevel.Error,
            context,
            payPalOrderId,
            expectedAmount,
            capturedAmount,
            null,
            reason,
            captureId: captureId);

        var refundResult = await _payPalService.RefundCaptureAsync(captureId, capturedAmount, cancellationToken);
        if (refundResult.Success)
        {
            _logger.LogWarning(
                "PayPal auto-refund succeeded. Flow={Flow} PayPalOrderId={PayPalOrderId} CaptureId={CaptureId} RefundId={RefundId} Expected={Expected} Captured={Captured} OrderId={OrderId} DepositId={DepositId} Reason={Reason}",
                context.Flow,
                payPalOrderId,
                captureId,
                refundResult.RefundId,
                expectedAmount,
                capturedAmount,
                context.OrderId,
                context.DepositId,
                reason);
        }
        else
        {
            _logger.LogCritical(
                "MANUAL_RECOVERY_REQUIRED PayPal auto-refund failed. Flow={Flow} PayPalOrderId={PayPalOrderId} CaptureId={CaptureId} Expected={Expected} Captured={Captured} OrderId={OrderId} DepositId={DepositId} Reason={Reason} RefundError={RefundError}. See docs/paypal-capture-recovery.md",
                context.Flow,
                payPalOrderId,
                captureId,
                expectedAmount,
                capturedAmount,
                context.OrderId,
                context.DepositId,
                reason,
                refundResult.ErrorMessage);
        }

        await AlertAdminsAsync(
            context,
            payPalOrderId,
            captureId,
            expectedAmount,
            capturedAmount,
            reason,
            refundResult.Success,
            refundResult.ErrorMessage,
            cancellationToken);

        return (true, refundResult.Success);
    }

    private async Task AlertAdminsAsync(
        PayPalCaptureContext context,
        string payPalOrderId,
        string captureId,
        decimal expectedAmount,
        decimal capturedAmount,
        string reason,
        bool refundSucceeded,
        string? refundError,
        CancellationToken cancellationToken)
    {
        var adminIds = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Admin)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            return;
        }

        var title = refundSucceeded
            ? _notifyLocalizer[NotificationKeys.PayPalAnomalyAutoRefundTitle]
            : _notifyLocalizer[NotificationKeys.PayPalAnomalyManualTitle];

        var message = _notifyLocalizer.Format(
            NotificationKeys.PayPalAnomalyMessage,
            context.Flow,
            payPalOrderId,
            captureId,
            expectedAmount,
            capturedAmount,
            reason,
            refundSucceeded,
            refundError ?? "none");

        foreach (var adminId in adminIds)
        {
            await _notificationService.CreateAndPushAsync(
                adminId,
                title,
                message,
                NotificationType.System,
                "/Admin/Dashboard",
                cancellationToken: cancellationToken);
        }
    }

    private void LogCorrelation(
        LogLevel level,
        PayPalCaptureContext context,
        string payPalOrderId,
        decimal expectedAmount,
        decimal? actualAmount,
        string? payPalStatus,
        string stage,
        string? detail = null,
        string? captureId = null)
    {
        _logger.Log(
            level,
            "PayPal capture guard. Stage={Stage} Flow={Flow} PayPalOrderId={PayPalOrderId} Expected={Expected} Actual={Actual} PayPalStatus={PayPalStatus} OrderId={OrderId} OrderIds={OrderIds} DepositId={DepositId} CaptureId={CaptureId} Detail={Detail}",
            stage,
            context.Flow,
            payPalOrderId,
            expectedAmount,
            actualAmount,
            payPalStatus,
            context.OrderId,
            context.OrderIds is null ? null : string.Join(',', context.OrderIds),
            context.DepositId,
            captureId,
            detail);
    }

    private static bool IsCapturableStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && CapturableStatuses.Contains(status);
}
