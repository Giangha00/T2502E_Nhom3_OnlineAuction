using System.Data;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderPaymentService : IOrderPaymentService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPayPalService _payPalService;
    private readonly ILogger<OrderPaymentService> _logger;

    public OrderPaymentService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        ILogger<OrderPaymentService> logger)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _logger = logger;
    }

    public async Task<PayPalCheckoutResult> InitiatePayPalCheckoutAsync(
        int buyerId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var orders = await GetPayableOrdersAsync(buyerId, cancellationToken);
        if (orders.Count == 0)
        {
            return PayPalCheckoutResult.Fail("No pending payment orders were found.");
        }

        if (orders.Any(order => string.IsNullOrWhiteSpace(order.ShippingAddress)))
        {
            return PayPalCheckoutResult.Fail("Please complete shipping information before paying with PayPal.");
        }

        var totalAmount = orders.Sum(order => order.TotalAmount);
        var referenceId = string.Join(',', orders.Select(order => order.OrderReference));

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

        var expectedAmount = orders
            .Where(order => order.Status == OrderStatuses.PendingPayment)
            .Sum(order => order.TotalAmount);

        var captureResult = await _payPalService.CaptureOrderAsync(payPalOrderId, cancellationToken);
        if (!captureResult.Success)
        {
            return PayPalCaptureCheckoutResult.Fail(captureResult.ErrorMessage ?? "Payment capture failed.");
        }

        if (!AmountsMatch(expectedAmount, captureResult.CapturedAmount))
        {
            _logger.LogWarning(
                "PayPal capture amount mismatch for buyer {BuyerId}. Expected {Expected}, got {Actual}.",
                buyerId,
                expectedAmount,
                captureResult.CapturedAmount);

            return PayPalCaptureCheckoutResult.Fail("Payment amount did not match the order total.");
        }

        var paidOrderIds = new List<int>();
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var order in orders.Where(order => order.Status == OrderStatuses.PendingPayment))
            {
                order.Status = OrderStatuses.Paid;
                order.PaymentMethod = "paypal";
                order.UpdatedAt = now;
                paidOrderIds.Add(order.Id);
            }

            foreach (var payment in pendingPayments.Where(payment => payment.Status == PaymentStatuses.Pending))
            {
                payment.Status = PaymentStatuses.Success;
                payment.TransactionId = captureResult.CaptureId;
                payment.PaidAt = now;
                payment.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

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

        var now = DateTime.UtcNow;
        foreach (var payment in pendingPayments)
        {
            payment.Status = PaymentStatuses.Cancelled;
            payment.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task<List<AuctionOrder>> GetPayableOrdersAsync(
        int buyerId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Orders
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline > now)
            .OrderBy(order => order.PaymentDeadline)
            .ToListAsync(cancellationToken);
    }

    private static bool AmountsMatch(decimal expected, decimal actual) =>
        Math.Abs(expected - actual) < 0.01m;
}
