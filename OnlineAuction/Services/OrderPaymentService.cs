using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderPaymentService : IOrderPaymentService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPayPalService _payPalService;
    private readonly IPayPalCaptureGuardService _payPalCaptureGuardService;
    private readonly ISandboxPayPalWalletService _sandboxPayPalWalletService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly IOrderService _orderService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ILogger<OrderPaymentService> _logger;
    private readonly PlatformFeeSettings _feeSettings;

    public OrderPaymentService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        IPayPalCaptureGuardService payPalCaptureGuardService,
        ISandboxPayPalWalletService sandboxPayPalWalletService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        IOrderService orderService,
        IRealtimePublisher realtimePublisher,
        ILogger<OrderPaymentService> logger,
        IOptions<PlatformFeeSettings> feeSettings)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _payPalCaptureGuardService = payPalCaptureGuardService;
        _sandboxPayPalWalletService = sandboxPayPalWalletService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _orderService = orderService;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
        _feeSettings = feeSettings.Value;
    }

    public async Task<PayPalCheckoutResult> InitiatePayPalCheckoutAsync(
        int buyerId,
        IReadOnlyList<int> selectedOrderIds,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingOrders = await _dbContext.Orders
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var selection = OrderCheckoutSelection.Resolve(pendingOrders, selectedOrderIds, now);
        if (!selection.Success)
        {
            return PayPalCheckoutResult.Fail(selection.Message);
        }

        var orders = selection.Orders;

        if (orders.Any(order => string.IsNullOrWhiteSpace(order.ShippingAddress)))
        {
            return PayPalCheckoutResult.Fail("Vui lòng điền đầy đủ thông tin giao hàng trước khi thanh toán PayPal.");
        }

        var totalAmount = orders.Sum(order => order.TotalAmount);
        // PayPal purchase_unit.reference_id rejects commas/spaces; keep a safe single token.
        // Multi-invoice checkouts use a compact checkout id (order mapping is stored on Payment rows).
        var referenceId = orders.Count == 1
            ? SanitizePayPalReferenceId(orders[0].OrderReference)
            : SanitizePayPalReferenceId($"CHK-{buyerId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

        // Do not block redirect to PayPal on the simulated sandbox wallet.
        // Wallet balance is enforced at capture (same pattern as registration deposits).
        var createResult = await _payPalService.CreateCheckoutOrderAsync(
            totalAmount,
            referenceId,
            returnUrl,
            cancelUrl,
            cancellationToken);

        if (!createResult.Success || string.IsNullOrWhiteSpace(createResult.PayPalOrderId))
        {
            return PayPalCheckoutResult.Fail(createResult.ErrorMessage ?? "Unable to start PayPal checkout.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var orderIds = orders.Select(order => order.Id).ToList();

            var stalePayments = await _dbContext.Payments
                .Where(payment =>
                    payment.Status == PaymentStatuses.Pending &&
                    orderIds.Contains(payment.OrderId))
                .ToListAsync(cancellationToken);

            if (stalePayments.Count > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var payment in stalePayments)
                {
                    payment.Status = PaymentStatuses.Cancelled;
                    payment.UpdatedAt = now;
                }
            }

            var createdAt = DateTime.UtcNow;
            foreach (var order in orders)
            {
                _dbContext.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Status = PaymentStatuses.Pending,
                    PayPalOrderId = createResult.PayPalOrderId,
                    CreatedAt = createdAt
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return PayPalCheckoutResult.Ok(createResult.ApprovalUrl!);
    }

    public async Task<PayPalCaptureCheckoutResult> CapturePayPalCheckoutAsync(
        int buyerId,
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return PayPalCaptureCheckoutResult.Fail("Missing PayPal checkout reference.");
        }

        var pendingPayments = await _dbContext.Payments
            .Include(payment => payment.Order)
                .ThenInclude(order => order.Items)
            .Where(payment =>
                payment.PayPalOrderId == payPalOrderId &&
                payment.Order.BuyerId == buyerId)
            .ToListAsync(cancellationToken);

        if (pendingPayments.Count == 0)
        {
            return PayPalCaptureCheckoutResult.Fail("Payment session was not found or does not belong to your account.");
        }

        var orders = pendingPayments
            .Select(payment => payment.Order)
            .DistinctBy(order => order.Id)
            .ToList();

        if (orders.All(order => order.Status == OrderStatuses.Paid))
        {
            return PayPalCaptureCheckoutResult.Ok(orders[0].Id, orders.Select(order => order.Id).ToList());
        }

        var payableOrders = orders
            .Where(order => order.Status == OrderStatuses.PendingPayment)
            .ToList();

        if (payableOrders.Count == 0)
        {
            _logger.LogWarning(
                "PayPal capture rejected because no payable orders remain. PayPalOrderId={PayPalOrderId} BuyerId={BuyerId} OrderIds={OrderIds}",
                payPalOrderId,
                buyerId,
                string.Join(',', orders.Select(order => order.Id)));

            return PayPalCaptureCheckoutResult.Fail(
                "These orders are no longer payable. Please return to My Orders and start checkout again.");
        }

        var expectedAmount = payableOrders.Sum(order => order.TotalAmount);

        var walletCheck = await _sandboxPayPalWalletService.EnsureSufficientBalanceAsync(
            buyerId,
            expectedAmount,
            cancellationToken);
        if (!walletCheck.Success)
        {
            return PayPalCaptureCheckoutResult.Fail(
                walletCheck.ErrorMessage ?? "Insufficient PayPal sandbox wallet balance.");
        }

        var captureContext = new PayPalCaptureContext(
            Flow: "order",
            UserId: buyerId,
            OrderId: payableOrders[0].Id,
            OrderIds: payableOrders.Select(order => order.Id).ToList());

        var captureResult = await _payPalCaptureGuardService.SafeCaptureAsync(
            payPalOrderId,
            expectedAmount,
            captureContext,
            cancellationToken);

        if (!captureResult.Success || string.IsNullOrWhiteSpace(captureResult.CaptureId))
        {
            return PayPalCaptureCheckoutResult.Fail(
                captureResult.ErrorMessage ?? "Payment capture failed.");
        }

        var paidOrderIds = new List<int>();
        string? persistenceFailureMessage = null;
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var reloadedPayments = await _dbContext.Payments
                .Include(payment => payment.Order)
                    .ThenInclude(order => order.Items)
                .Where(payment =>
                    payment.PayPalOrderId == payPalOrderId &&
                    payment.Order.BuyerId == buyerId)
                .ToListAsync(cancellationToken);

            var reloadedOrders = reloadedPayments
                .Select(payment => payment.Order)
                .DistinctBy(order => order.Id)
                .ToList();

            if (reloadedOrders.All(order => order.Status == OrderStatuses.Paid))
            {
                paidOrderIds.AddRange(reloadedOrders.Select(order => order.Id));
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var stillPayableOrders = reloadedOrders
                .Where(order => order.Status == OrderStatuses.PendingPayment)
                .ToList();

            if (stillPayableOrders.Count == 0)
            {
                persistenceFailureMessage =
                    "Payment could not be applied because the order is no longer payable.";
                return;
            }

            var reloadedExpectedAmount = stillPayableOrders.Sum(order => order.TotalAmount);
            if (!PayPalAmountHelper.AmountsMatch(reloadedExpectedAmount, captureResult.CapturedAmount))
            {
                persistenceFailureMessage =
                    "Payment amount changed while completing checkout. A refund has been initiated.";
                return;
            }

            var deductResult = await _sandboxPayPalWalletService.TryDeductAsync(
                buyerId,
                reloadedExpectedAmount,
                cancellationToken);
            if (!deductResult.Success)
            {
                persistenceFailureMessage = deductResult.ErrorMessage
                    ?? "Insufficient PayPal sandbox wallet balance.";
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var order in stillPayableOrders)
            {
                order.Status = OrderStatuses.Paid;
                order.PaymentMethod = "paypal";
                order.UpdatedAt = now;
                MarketplaceFeeCalculator.ApplySellerSettlement(order, _feeSettings);
                paidOrderIds.Add(order.Id);

                var auctionId = order.Items.First().AuctionId;

                var winnerDeposit = await _dbContext.AuctionRegistrationDeposits
                    .FirstOrDefaultAsync(d =>
                            d.AuctionId == auctionId &&
                            d.UserId == order.BuyerId &&
                            d.Status == AuctionRegistrationDepositStatuses.Paid,
                        cancellationToken);

                if (winnerDeposit != null)
                {
                    winnerDeposit.Status = AuctionRegistrationDepositStatuses.Applied;
                    winnerDeposit.UpdatedAt = now;
                }
            }

            foreach (var payment in reloadedPayments.Where(payment => payment.Status == PaymentStatuses.Pending))
            {
                payment.Status = PaymentStatuses.Success;
                payment.TransactionId = captureResult.CaptureId;
                payment.PaidAt = now;
                payment.UpdatedAt = now;
            }

            var paidOrders = stillPayableOrders.Where(order => paidOrderIds.Contains(order.Id)).ToList();
            await OrderCancellationHelper.MarkAuctionsCompletedAfterPaymentAsync(
                _dbContext,
                paidOrders,
                now,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        if (!string.IsNullOrWhiteSpace(persistenceFailureMessage))
        {
            var recovery = await _payPalService.RefundCaptureAsync(
                captureResult.CaptureId,
                captureResult.CapturedAmount,
                cancellationToken);

            _logger.LogCritical(
                "MANUAL_RECOVERY_REQUIRED PayPal order capture could not be persisted. PayPalOrderId={PayPalOrderId} CaptureId={CaptureId} RefundSucceeded={RefundSucceeded} RefundError={RefundError} Reason={Reason}",
                payPalOrderId,
                captureResult.CaptureId,
                recovery.Success,
                recovery.ErrorMessage,
                persistenceFailureMessage);

            return PayPalCaptureCheckoutResult.Fail(persistenceFailureMessage);
        }

        if (paidOrderIds.Count == 0)
        {
            return PayPalCaptureCheckoutResult.Fail(
                "Payment could not be applied because the order is no longer payable.");
        }

        foreach (var orderId in paidOrderIds)
        {
            await _notificationService.CreateAndPushAsync(
                buyerId,
                _notifyLocalizer[NotificationKeys.PaymentSuccessTitle],
                _notifyLocalizer[NotificationKeys.PaymentSuccessSimpleMessage],
                NotificationType.Payment,
                $"/Payment/Confirmation?orderId={orderId}",
                NotificationReferenceTypes.PaymentSuccess,
                orderId);

            await OrderNotificationHelper.NotifySellerPaymentReceivedAsync(
                _notificationService,
                _notifyLocalizer,
                _dbContext,
                orderId,
                "PayPal");
        }

        var orderCount = await _orderService.CountPendingPaymentOrdersAsync(buyerId);
        await _realtimePublisher.SendOrderCountToUserAsync(buyerId, orderCount);

        return PayPalCaptureCheckoutResult.Ok(paidOrderIds[0], paidOrderIds);
    }

    public async Task CancelPayPalCheckoutAsync(
        int buyerId,
        string? payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return;
        }

        var pendingPayments = await _dbContext.Payments
            .Include(payment => payment.Order)
            .Where(payment =>
                payment.PayPalOrderId == payPalOrderId &&
                payment.Status == PaymentStatuses.Pending &&
                payment.Order.BuyerId == buyerId)
            .ToListAsync(cancellationToken);

        if (pendingPayments.Count == 0)
        {
            return;
        }

        var cancelResult = await _payPalService.CancelOrderAsync(payPalOrderId, cancellationToken);
        if (!cancelResult.Success)
        {
            _logger.LogWarning(
                "PayPal checkout cancel failed for order {PayPalOrderId}: {ErrorMessage}",
                payPalOrderId,
                cancelResult.ErrorMessage);
        }

        var now = DateTime.UtcNow;
        foreach (var payment in pendingPayments)
        {
            payment.Status = PaymentStatuses.Cancelled;
            payment.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CancelAllStalePayPalSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-1);
        var stalePayments = await _dbContext.Payments
            .Include(payment => payment.Order)
            .Where(payment =>
                payment.Status == PaymentStatuses.Pending &&
                payment.PayPalOrderId != null &&
                payment.CreatedAt <= staleThreshold)
            .ToListAsync(cancellationToken);

        var staleDeposits = await _dbContext.AuctionRegistrationDeposits
            .Include(deposit => deposit.Registration)
            .Where(deposit =>
                deposit.Status == AuctionRegistrationDepositStatuses.Pending &&
                deposit.PayPalOrderId != null &&
                deposit.CreatedAt <= staleThreshold)
            .ToListAsync(cancellationToken);

        var payPalOrderIds = stalePayments
            .Select(payment => payment.PayPalOrderId!)
            .Concat(staleDeposits.Select(deposit => deposit.PayPalOrderId!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var payPalOrderIdValue in payPalOrderIds)
        {
            var cancelResult = await _payPalService.CancelOrderAsync(payPalOrderIdValue, cancellationToken);
            if (!cancelResult.Success)
            {
                _logger.LogWarning(
                    "PayPal stale session cancel failed for order {PayPalOrderId}: {ErrorMessage}",
                    payPalOrderIdValue,
                    cancelResult.ErrorMessage);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var payment in stalePayments)
        {
            payment.Status = PaymentStatuses.Cancelled;
            payment.UpdatedAt = now;
        }

        foreach (var deposit in staleDeposits)
        {
            deposit.Status = AuctionRegistrationDepositStatuses.Cancelled;
            deposit.UpdatedAt = now;
            deposit.Registration.Status = AuctionRegistrationStatuses.Cancelled;
            deposit.Registration.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return stalePayments.Count + staleDeposits.Count;
    }

    public async Task<PaymentConfirmationViewModel?> GetPaidOrderConfirmationAsync(
        int buyerId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item =>
                item.Id == orderId &&
                item.BuyerId == buyerId &&
                item.DeletedAt == null,
                cancellationToken);

        if (order is null || order.Status != OrderStatuses.Paid)
        {
            return null;
        }

        var successfulPayment = order.Payments
            .Where(payment => payment.Status == PaymentStatuses.Success)
            .OrderByDescending(payment => payment.PaidAt)
            .FirstOrDefault();

        if (successfulPayment is null)
        {
            return null;
        }

        var relatedOrders = await _dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .Where(item =>
                item.BuyerId == buyerId &&
                item.Status == OrderStatuses.Paid &&
                item.DeletedAt == null &&
                item.Payments.Any(payment =>
                    payment.Status == PaymentStatuses.Success &&
                    payment.TransactionId == successfulPayment.TransactionId))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var items = relatedOrders
            .SelectMany(relatedOrder => relatedOrder.Items)
            .Select(item => new PaymentConfirmationItem
            {
                Name = item.ItemName,
                Grade = item.ItemGrade ?? string.Empty,
                ImageUrl = item.ItemImageUrl ?? string.Empty,
                Amount = item.WinningBid
            })
            .ToList();

        var totalAmount = relatedOrders.Sum(relatedOrder => relatedOrder.TotalAmount);

        return new PaymentConfirmationViewModel
        {
            OrderId = order.Id,
            OrderReference = relatedOrders.Count == 1
                ? order.OrderReference
                : string.Join(", ", relatedOrders.Select(relatedOrder => relatedOrder.OrderReference)),
            AuctionName = items.Count == 1
                ? items[0].Name
                : $"{items.Count} auction items",
            TotalAmount = totalAmount,
            PaymentMethod = "PayPal",
            PaidAt = successfulPayment.PaidAt ?? order.UpdatedAt ?? order.CreatedAt,
            TransactionId = successfulPayment.TransactionId ?? string.Empty,
            Items = items
        };
    }

public async Task<PayPalWebhookProcessingResult> ProcessPayPalWebhookAsync(
        string requestBody,
        IHeaderDictionary headers,
        CancellationToken cancellationToken = default)
    {
        var verifyResult = await _payPalService.VerifyWebhookSignatureAsync(requestBody, headers, cancellationToken);
        if (!verifyResult.Success || !verifyResult.Verified)
        {
            _logger.LogWarning("PayPal webhook verification failed: {ErrorMessage}", verifyResult.ErrorMessage);
            return PayPalWebhookProcessingResult.Fail(verifyResult.ErrorMessage ?? "PayPal webhook verification failed.");
        }

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return PayPalWebhookProcessingResult.Fail("Empty PayPal webhook payload.");
        }

        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(requestBody);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "PayPal webhook payload is not valid JSON.");
            return PayPalWebhookProcessingResult.Fail("Invalid PayPal webhook payload.");
        }

        var root = payload.RootElement;
        if (!root.TryGetProperty("event_type", out var eventTypeElement))
        {
            return PayPalWebhookProcessingResult.Fail("PayPal webhook payload missing event_type.");
        }

        var eventType = eventTypeElement.GetString() ?? string.Empty;
        if (!root.TryGetProperty("resource", out var resource))
        {
            return PayPalWebhookProcessingResult.Fail("PayPal webhook payload missing resource.");
        }

        if (!eventType.Equals("PAYMENT.CAPTURE.COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("PayPal webhook ignored event type {EventType}.", eventType);
            return PayPalWebhookProcessingResult.Ok();
        }

        if (!TryGetNestedString(resource, new[] { "supplementary_data", "related_ids", "order_id" }, out var payPalOrderId)
            && !TryGetNestedString(resource, new[] { "order_id" }, out payPalOrderId))
        {
            _logger.LogWarning("PayPal webhook capture event missing order_id.");
            return PayPalWebhookProcessingResult.Fail("PayPal webhook event missing PayPal order id.");
        }

        if (!TryGetNestedString(resource, new[] { "id" }, out var captureId))
        {
            _logger.LogWarning("PayPal webhook capture event missing capture id.");
            return PayPalWebhookProcessingResult.Fail("PayPal webhook event missing capture id.");
        }

        if (!TryGetNestedString(resource, new[] { "amount", "value" }, out var amountValue)
            || !decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var captureAmount))
        {
            _logger.LogWarning("PayPal webhook capture event has invalid amount. Value={AmountValue}.", amountValue);
            return PayPalWebhookProcessingResult.Fail("PayPal webhook capture amount is invalid.");
        }

        var payments = await _dbContext.Payments
            .Include(payment => payment.Order)
                .ThenInclude(order => order.Items)
            .Where(payment => payment.PayPalOrderId == payPalOrderId)
            .ToListAsync(cancellationToken);

        if (payments.Count == 0)
        {
            _logger.LogWarning("No payment records found for PayPal order {PayPalOrderId}.", payPalOrderId);
            return PayPalWebhookProcessingResult.Ok();
        }

        var pendingPayments = payments.Where(payment => payment.Status == PaymentStatuses.Pending).ToList();
        if (pendingPayments.Count == 0)
        {
            _logger.LogInformation("PayPal webhook ignored already-processed order {PayPalOrderId}.", payPalOrderId);
            return PayPalWebhookProcessingResult.Ok();
        }

        var now = DateTime.UtcNow;
        var orders = pendingPayments
            .Select(payment => payment.Order)
            .DistinctBy(order => order.Id)
            .ToList();

        var expectedAmount = orders
            .Where(order => order.Status == OrderStatuses.PendingPayment)
            .Sum(order => order.TotalAmount);

        if (expectedAmount <= 0)
        {
            _logger.LogWarning(
                "PayPal webhook ignored because no payable orders remain. PayPalOrderId={PayPalOrderId}",
                payPalOrderId);
            return PayPalWebhookProcessingResult.Ok();
        }

        if (!PayPalAmountHelper.AmountsMatch(expectedAmount, captureAmount))
        {
            _logger.LogWarning(
                "PayPal webhook capture amount mismatch for order {PayPalOrderId}. Expected={Expected}, Actual={Actual}.",
                payPalOrderId,
                expectedAmount,
                captureAmount);
            return PayPalWebhookProcessingResult.Fail("Payment amount did not match order total.");
        }

        var webhookBuyerId = orders[0].BuyerId;
        var webhookWalletCheck = await _sandboxPayPalWalletService.EnsureSufficientBalanceAsync(
            webhookBuyerId,
            expectedAmount,
            cancellationToken);
        if (!webhookWalletCheck.Success)
        {
            _logger.LogWarning(
                "PayPal webhook rejected due to insufficient sandbox wallet. PayPalOrderId={PayPalOrderId} BuyerId={BuyerId}",
                payPalOrderId,
                webhookBuyerId);
            return PayPalWebhookProcessingResult.Fail(
                webhookWalletCheck.ErrorMessage ?? "Insufficient PayPal sandbox wallet balance.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        var walletDeductFailed = false;
        string? walletDeductError = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var payableOrders = orders.Where(order => order.Status == OrderStatuses.PendingPayment).ToList();
            var amountToDeduct = payableOrders.Sum(order => order.TotalAmount);
            if (amountToDeduct > 0)
            {
                var deductResult = await _sandboxPayPalWalletService.TryDeductAsync(
                    webhookBuyerId,
                    amountToDeduct,
                    cancellationToken);
                if (!deductResult.Success)
                {
                    walletDeductFailed = true;
                    walletDeductError = deductResult.ErrorMessage;
                    return;
                }
            }

            foreach (var order in payableOrders)
            {
                order.Status = OrderStatuses.Paid;
                order.PaymentMethod = "paypal";
                order.UpdatedAt = now;
                MarketplaceFeeCalculator.ApplySellerSettlement(order, _feeSettings);

                var auctionId = order.Items.First().AuctionId;
                var winnerDeposit = await _dbContext.AuctionRegistrationDeposits
                    .FirstOrDefaultAsync(d =>
                        d.AuctionId == auctionId &&
                        d.UserId == order.BuyerId &&
                        d.Status == AuctionRegistrationDepositStatuses.Paid,
                        cancellationToken);

                if (winnerDeposit != null)
                {
                    winnerDeposit.Status = AuctionRegistrationDepositStatuses.Applied;
                    winnerDeposit.UpdatedAt = now;
                }
            }

            foreach (var payment in pendingPayments)
            {
                payment.Status = PaymentStatuses.Success;
                payment.TransactionId = captureId;
                payment.PaidAt = now;
                payment.UpdatedAt = now;
            }

            var paidOrders = orders.Where(order => order.Status == OrderStatuses.Paid).ToList();
            await OrderCancellationHelper.MarkAuctionsCompletedAfterPaymentAsync(
                _dbContext,
                paidOrders,
                now,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        if (walletDeductFailed)
        {
            return PayPalWebhookProcessingResult.Fail(
                walletDeductError ?? "Insufficient PayPal sandbox wallet balance.");
        }

        foreach (var order in orders.Where(order => order.Status == OrderStatuses.Paid))
        {
            await _notificationService.CreateAndPushAsync(
                order.BuyerId,
                _notifyLocalizer[NotificationKeys.PaymentSuccessTitle],
                _notifyLocalizer[NotificationKeys.PaymentSuccessSimpleMessage],
                NotificationType.Payment,
                $"/Payment/Confirmation?orderId={order.Id}",
                NotificationReferenceTypes.PaymentSuccess,
                order.Id);

            await OrderNotificationHelper.NotifySellerPaymentReceivedAsync(
                _notificationService,
                _notifyLocalizer,
                _dbContext,
                order.Id,
                "PayPal",
                cancellationToken);
        }

        var buyerIds = orders.Select(order => order.BuyerId).Distinct();
        foreach (var buyerId in buyerIds)
        {
            var orderCount = await _orderService.CountPendingPaymentOrdersAsync(buyerId);
            await _realtimePublisher.SendOrderCountToUserAsync(buyerId, orderCount, cancellationToken);
        }

        return PayPalWebhookProcessingResult.Ok();

        static bool TryGetNestedString(JsonElement parent, string[] path, out string value)
        {
            value = string.Empty;
            var current = parent;
            foreach (var property in path)
            {
                if (!current.TryGetProperty(property, out var child))
                {
                    value = string.Empty;
                    return false;
                }

                current = child;
            }

            value = current.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    public async Task<string> TestProcessIpnAsync(
        string payPalOrderId,
        string transactionId,
        string paymentStatus,
        CancellationToken cancellationToken = default)
    {
        /*
         * Hàm này dùng để xử lý IPN.
         *
         * Đầu vào:
         *
         * payPalOrderId
         * Mã Order bên PayPal.
         *
         * transactionId
         * Mã giao dịch PayPal.
         *
         * paymentStatus
         * Completed / Pending / Failed...
         */

        // Kiểm tra dữ liệu đầu vào
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return "Thiếu PayPalOrderId";
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return "Thiếu TransactionId";
        }

        /*
         * Tìm Payment theo PayPalOrderId.
         *
         * Include(Order)
         * nghĩa là lấy luôn Order liên kết với Payment.
         */

        var payments = await _dbContext.Payments
            .Include(x => x.Order)
            .Where(x => x.PayPalOrderId == payPalOrderId)
            .ToListAsync(cancellationToken);

        /*
         * Nếu không tìm thấy Payment
         */

        if (payments.Count == 0)
        {
            return "Không tìm thấy Payment";
        }

        /*
         * Chống xử lý trùng.
         *
         * PayPal có thể gửi IPN nhiều lần.
         * Nếu TransactionId đã tồn tại thì không xử lý nữa.
         */

        var transactionExists = await _dbContext.Payments
            .AnyAsync(
                x => x.TransactionId == transactionId,
                cancellationToken);

        if (transactionExists)
        {
            return "Transaction đã xử lý";
        }

        /*
         * Lấy thời gian hiện tại UTC
         */

        var now = DateTime.UtcNow;

        /*
         * Nếu PayPal báo thanh toán thành công
         */

        if (paymentStatus == "Completed")
        {
            foreach (var payment in payments)
            {
                /*
                 * Cập nhật Payment
                 */

                payment.Status = PaymentStatuses.Success;

                payment.TransactionId = transactionId;

                payment.PaidAt = now;

                payment.UpdatedAt = now;

                /*
                 * Cập nhật Order
                 */

                payment.Order.Status = OrderStatuses.Paid;

                payment.Order.PaymentMethod = "paypal";
                MarketplaceFeeCalculator.ApplySellerSettlement(payment.Order, _feeSettings);

                payment.Order.UpdatedAt = now;
            }

            /*
             * SaveChanges
             * lưu tất cả thay đổi xuống database
             */

            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var payment in payments)
            {
                await _notificationService.CreateAndPushAsync(
                    payment.Order.BuyerId,
                    _notifyLocalizer[NotificationKeys.PaymentSuccessTitle],
                    _notifyLocalizer[NotificationKeys.PaymentSuccessSimpleMessage],
                    NotificationType.Payment,
                    $"/Payment/Confirmation?orderId={payment.OrderId}",
                    NotificationReferenceTypes.PaymentSuccess,
                    payment.OrderId,
                    cancellationToken: cancellationToken);

                await OrderNotificationHelper.NotifySellerPaymentReceivedAsync(
                    _notificationService,
                    _notifyLocalizer,
                    _dbContext,
                    payment.OrderId,
                    "PayPal",
                    cancellationToken);
            }

            var buyerIds = payments.Select(p => p.Order.BuyerId).Distinct();
            foreach (var buyerId in buyerIds)
            {
                var orderCount = await _orderService.CountPendingPaymentOrdersAsync(buyerId);
                await _realtimePublisher.SendOrderCountToUserAsync(buyerId, orderCount, cancellationToken);
            }

            return "Thanh toán thành công";
        }

        /*
         * Payment Pending
         */

        if (paymentStatus == "Pending")
        {
            foreach (var payment in payments)
            {
                payment.Status = PaymentStatuses.Pending;

                payment.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return "Thanh toán đang chờ";
        }

        /*
         * Payment thất bại
         */

        if (paymentStatus == "Failed"
            || paymentStatus == "Denied")
        {
            foreach (var payment in payments)
            {
                payment.Status = PaymentStatuses.Failed;

                payment.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return "Thanh toán thất bại";
        }

        return $"Chưa xử lý status: {paymentStatus}";
    }

    private static string SanitizePayPalReferenceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"CHK-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        var chars = value
            .Trim()
            .Select(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray();

        var sanitized = new string(chars).Trim('-');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"CHK-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        return sanitized.Length <= 127 ? sanitized : sanitized[..127];
    }
}
