using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class RefundComplaintService : IRefundComplaintService
{
    private const int RefundWindowDays = 14;

    private static readonly string[] EligibleOrderStatuses =
    [
        OrderStatuses.Paid,
        OrderStatuses.Shipped,
        OrderStatuses.Delivered
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<RefundComplaintService> _logger;

    public RefundComplaintService(
        AuctionHouseDbContext dbContext,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        ILogger<RefundComplaintService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RefundEligibleOrderViewModel>> GetEligibleOrdersAsync(
        int buyerId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.DeletedAt == null
                && order.BuyerId == buyerId
                && EligibleOrderStatuses.Contains(order.Status))
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .OrderByDescending(order => order.UpdatedAt ?? order.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(order => order.Id).ToList();
        var openComplaintOrderIds = await _dbContext.Complaints
            .AsNoTracking()
            .Where(complaint =>
                complaint.DeletedAt == null
                && complaint.OrderId.HasValue
                && orderIds.Contains(complaint.OrderId.Value)
                && ComplaintStatuses.OpenStatuses.Contains(complaint.Status))
            .Select(complaint => complaint.OrderId!.Value)
            .ToListAsync(cancellationToken);

        var openComplaintSet = openComplaintOrderIds.ToHashSet();

        return orders
            .Where(order => !openComplaintSet.Contains(order.Id))
            .Where(order => IsWithinRefundWindow(order, utcNow))
            .Select(order =>
            {
                var paidAt = GetSuccessfulPaymentDate(order);
                return new RefundEligibleOrderViewModel
                {
                    OrderId = order.Id,
                    OrderReference = order.OrderReference,
                    AuctionName = order.Items.OrderBy(item => item.Id).FirstOrDefault()?.ItemName ?? "Auction order",
                    AmountPaid = order.TotalAmount,
                    PaidOn = paidAt ?? order.CreatedAt
                };
            })
            .ToList();
    }

    public async Task<(bool Success, string Message, string? RequestReference)> SubmitAsync(
        int buyerId,
        RefundSubmitViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.ContactName))
        {
            return (false, "Full name is required.", null);
        }

        if (string.IsNullOrWhiteSpace(model.ContactEmail) || !model.ContactEmail.Contains('@'))
        {
            return (false, "A valid email address is required.", null);
        }

        if (string.IsNullOrWhiteSpace(model.ReasonCode)
            || !ComplaintReasonCodes.Labels.ContainsKey(model.ReasonCode))
        {
            return (false, "Please select a valid refund reason.", null);
        }

        if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Trim().Length < 20)
        {
            return (false, "Please provide a detailed description (at least 20 characters).", null);
        }

        AuctionOrder? order = null;
        string? orderReference = null;

        if (model.OrderId.HasValue)
        {
            order = await _dbContext.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(
                    o => o.Id == model.OrderId.Value
                         && o.DeletedAt == null
                         && o.BuyerId == buyerId,
                    cancellationToken);

            if (order is null)
            {
                return (false, "The selected order was not found or does not belong to your account.", null);
            }

            orderReference = order.OrderReference;

            if (!string.IsNullOrWhiteSpace(model.OrderReference)
                && !string.Equals(order.OrderReference, model.OrderReference.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Order reference does not match the selected order.", null);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.OrderReference))
        {
            orderReference = model.OrderReference.Trim();

            order = await _dbContext.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(
                    o => o.DeletedAt == null
                         && o.BuyerId == buyerId
                         && o.OrderReference == orderReference,
                    cancellationToken);
        }
        else
        {
            return (false, "Please select an order or enter an order reference.", null);
        }

        if (order is not null)
        {
            if (!EligibleOrderStatuses.Contains(order.Status))
            {
                return (false, "Refund requests are only available for paid orders.", null);
            }

            if (!IsWithinRefundWindow(order, DateTime.UtcNow))
            {
                return (false, "Refund requests must be submitted within 14 days of delivery or expected delivery.", null);
            }

            var hasOpenComplaint = await _dbContext.Complaints.AnyAsync(
                complaint => complaint.DeletedAt == null
                             && complaint.OrderId == order.Id
                             && ComplaintStatuses.OpenStatuses.Contains(complaint.Status),
                cancellationToken);

            if (hasOpenComplaint)
            {
                return (false, "A refund request for this order is already pending review.", null);
            }
        }

        if (model.RequestedAmount.HasValue && model.RequestedAmount.Value <= 0)
        {
            return (false, "Requested amount must be greater than zero when specified.", null);
        }

        if (order is not null
            && model.RequestedAmount.HasValue
            && model.RequestedAmount.Value > order.TotalAmount)
        {
            return (false, "Requested amount cannot exceed the order total.", null);
        }

        var evidenceResult = SerializeEvidenceUrls(model.EvidenceUrls);
        if (!evidenceResult.Success)
        {
            return (false, evidenceResult.Message, null);
        }

        var now = DateTime.UtcNow;
        var complaint = new Complaint
        {
            RequestReference = $"RF-TEMP-{Guid.NewGuid():N}"[..32],
            OrderId = order?.Id,
            OrderReference = orderReference,
            BuyerId = buyerId,
            ComplaintType = ComplaintTypes.Refund,
            ReasonCode = model.ReasonCode,
            Description = model.Description.Trim(),
            RequestedAmount = model.RequestedAmount,
            ContactName = model.ContactName.Trim(),
            ContactEmail = model.ContactEmail.Trim(),
            Status = ComplaintStatuses.Pending,
            EvidenceUrlsJson = evidenceResult.Json,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Complaints.Add(complaint);
        await _dbContext.SaveChangesAsync(cancellationToken);

        complaint.RequestReference = Complaint.BuildRequestReference(complaint.Id, complaint.CreatedAt);
        complaint.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await TryNotifySubmittedAsync(complaint, cancellationToken);

        return (true, "Your refund request has been submitted.", complaint.RequestReference);
    }

    public async Task<RefundConfirmationViewModel?> GetConfirmationAsync(
        int buyerId,
        string requestReference,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _dbContext.Complaints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.DeletedAt == null
                     && c.BuyerId == buyerId
                     && c.RequestReference == requestReference,
                cancellationToken);

        if (complaint is null)
        {
            return null;
        }

        return new RefundConfirmationViewModel
        {
            RequestId = complaint.RequestReference,
            OrderReference = complaint.OrderReference ?? "N/A",
            Reason = ComplaintReasonCodes.Labels.TryGetValue(complaint.ReasonCode, out var label)
                ? label
                : complaint.ReasonCode
        };
    }

    private async Task TryNotifySubmittedAsync(Complaint complaint, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAndPushAsync(
                complaint.BuyerId,
                _notifyLocalizer[NotificationKeys.RefundSubmittedTitle],
                _notifyLocalizer.Format(NotificationKeys.RefundSubmittedMessage, complaint.RequestReference),
                NotificationType.Refund,
                $"/Refund/Confirmation?requestId={Uri.EscapeDataString(complaint.RequestReference)}",
                NotificationReferenceTypes.RefundRequested,
                complaint.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify buyer about submitted complaint {ComplaintId}", complaint.Id);
        }
    }

    private static bool IsWithinRefundWindow(AuctionOrder order, DateTime utcNow)
    {
        var referenceDate = GetRefundReferenceDate(order);
        if (!referenceDate.HasValue)
        {
            return false;
        }

        return utcNow <= referenceDate.Value.AddDays(RefundWindowDays);
    }

    private static DateTime? GetRefundReferenceDate(AuctionOrder order)
    {
        var paidAt = GetSuccessfulPaymentDate(order);

        if (order.Status is OrderStatuses.Delivered or OrderStatuses.Shipped)
        {
            return order.UpdatedAt ?? paidAt ?? order.CreatedAt;
        }

        return paidAt ?? order.UpdatedAt ?? order.CreatedAt;
    }

    private static DateTime? GetSuccessfulPaymentDate(AuctionOrder order) =>
        order.Payments
            .Where(payment => payment.DeletedAt == null && payment.Status == PaymentStatuses.Success)
            .OrderByDescending(payment => payment.PaidAt)
            .Select(payment => payment.PaidAt)
            .FirstOrDefault();

    private static (bool Success, string Message, string? Json) SerializeEvidenceUrls(string? rawEvidenceUrls)
    {
        if (string.IsNullOrWhiteSpace(rawEvidenceUrls))
        {
            return (true, string.Empty, null);
        }

        var urls = rawEvidenceUrls
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (urls.Count > 5)
        {
            return (false, "Please provide no more than 5 evidence links.", null);
        }

        foreach (var url in urls)
        {
            if (url.Length > 500
                || !Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl)
                || parsedUrl.Scheme is not ("http" or "https"))
            {
                return (false, "Evidence links must be valid http or https URLs.", null);
            }
        }

        return (true, string.Empty, JsonSerializer.Serialize(urls));
    }
}
