using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Complaints;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminComplaintService : IAdminComplaintService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdminComplaintService> _logger;

    public AdminComplaintService(
        AuctionHouseDbContext dbContext,
        INotificationService notificationService,
        ILogger<AdminComplaintService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Complaints.AsNoTracking()
            .CountAsync(
                complaint => complaint.DeletedAt == null && complaint.Status == ComplaintStatuses.Pending,
                cancellationToken);

    public async Task<ComplaintListViewModel> GetComplaintsAsync(
        ComplaintFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Complaints
            .AsNoTracking()
            .Where(complaint => complaint.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(complaint =>
                complaint.RequestReference.Contains(keyword)
                || (complaint.OrderReference != null && complaint.OrderReference.Contains(keyword))
                || complaint.Buyer.FullName.Contains(keyword)
                || complaint.ContactEmail.Contains(keyword)
                || (complaint.Buyer.Email != null && complaint.Buyer.Email.Contains(keyword))
                || (complaint.Order != null && complaint.Order.OrderReference.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(complaint => complaint.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.ReasonCode))
        {
            query = query.Where(complaint => complaint.ReasonCode == filter.ReasonCode);
        }

        if (!string.IsNullOrWhiteSpace(filter.ComplaintType))
        {
            query = query.Where(complaint => complaint.ComplaintType == filter.ComplaintType);
        }

        var dateRange = ParseDateRange(filter.DateRange);
        if (dateRange.StartDate.HasValue && dateRange.EndDate.HasValue)
        {
            query = query.Where(complaint =>
                complaint.CreatedAt >= dateRange.StartDate.Value
                && complaint.CreatedAt < dateRange.EndDate.Value);
        }

        query = filter.SortOrder switch
        {
            "date_asc" => query.OrderBy(complaint => complaint.CreatedAt),
            _ => query.OrderByDescending(complaint => complaint.CreatedAt)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(complaint => new ComplaintListItemViewModel
            {
                Id = complaint.Id,
                RequestReference = complaint.RequestReference,
                OrderReference = complaint.OrderReference ?? (complaint.Order != null ? complaint.Order.OrderReference : null),
                BuyerName = complaint.Buyer.FullName,
                BuyerEmail = complaint.ContactEmail,
                ProductName = complaint.Order != null && complaint.Order.Items.Any()
                    ? complaint.Order.Items.OrderBy(item => item.Id).First().ItemName
                    : "—",
                ReasonCode = complaint.ReasonCode,
                ReasonLabel = complaint.ReasonCode,
                RequestedAmount = complaint.RequestedAmount,
                Status = complaint.Status,
                StatusLabel = complaint.Status,
                SubmittedAt = complaint.CreatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.ReasonLabel = ComplaintDisplayHelper.GetReasonLabel(item.ReasonCode);
            item.StatusLabel = ComplaintDisplayHelper.GetStatusLabel(item.Status);
        }

        return new ComplaintListViewModel
        {
            Items = items,
            Filter = filter,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ComplaintDetailViewModel?> GetComplaintDetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _dbContext.Complaints
            .AsNoTracking()
            .Include(c => c.Buyer)
            .Include(c => c.Reviewer)
            .Include(c => c.Order!)
                .ThenInclude(o => o.Items)
                .ThenInclude(i => i.Auction)
                .ThenInclude(a => a.Product)
                .ThenInclude(p => p.Seller)
            .Include(c => c.Order!)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);

        if (complaint is null)
        {
            return null;
        }

        var firstItem = complaint.Order?.Items.OrderBy(item => item.Id).FirstOrDefault();
        var seller = firstItem?.Auction.Product.Seller;
        var successfulPayment = complaint.Order?.Payments
            .Where(payment => payment.DeletedAt == null && payment.Status == PaymentStatuses.Success)
            .OrderByDescending(payment => payment.PaidAt)
            .FirstOrDefault();

        var hasApprovedForOrder = complaint.OrderId.HasValue && await _dbContext.Complaints
            .AsNoTracking()
            .AnyAsync(
                c => c.DeletedAt == null
                     && c.OrderId == complaint.OrderId
                     && c.Id != complaint.Id
                     && c.Status == ComplaintStatuses.Approved,
                cancellationToken);

        var orderIsPaid = complaint.Order == null
                          || complaint.Order.Status == OrderStatuses.Paid
                          || complaint.Order.Status == OrderStatuses.Shipped
                          || complaint.Order.Status == OrderStatuses.Delivered;

        var isOpen = complaint.Status is ComplaintStatuses.Pending or ComplaintStatuses.UnderReview;

        return new ComplaintDetailViewModel
        {
            Id = complaint.Id,
            RequestReference = complaint.RequestReference,
            ComplaintType = complaint.ComplaintType,
            ComplaintTypeLabel = ComplaintDisplayHelper.GetTypeLabel(complaint.ComplaintType),
            ReasonCode = complaint.ReasonCode,
            ReasonLabel = ComplaintDisplayHelper.GetReasonLabel(complaint.ReasonCode),
            Description = complaint.Description,
            RequestedAmount = complaint.RequestedAmount,
            Status = complaint.Status,
            StatusLabel = ComplaintDisplayHelper.GetStatusLabel(complaint.Status),
            ContactName = complaint.ContactName,
            ContactEmail = complaint.ContactEmail,
            AdminNotes = complaint.AdminNotes,
            ResolutionNote = complaint.ResolutionNote,
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt,
            ReviewedAt = complaint.ReviewedAt,
            ReviewerName = complaint.Reviewer?.FullName,
            EvidenceUrls = ParseEvidenceUrls(complaint.EvidenceUrlsJson),
            OrderId = complaint.OrderId,
            OrderReference = complaint.OrderReference ?? complaint.Order?.OrderReference,
            OrderSubtotal = complaint.Order?.Subtotal,
            OrderTotal = complaint.Order?.TotalAmount,
            OrderStatus = complaint.Order?.Status,
            PaymentMethod = complaint.Order?.PaymentMethod,
            PaidAt = successfulPayment?.PaidAt,
            BuyerId = complaint.BuyerId,
            BuyerName = complaint.Buyer.FullName,
            BuyerEmail = complaint.ContactEmail,
            SellerId = seller?.Id,
            SellerName = seller?.FullName,
            SellerEmail = seller?.Email,
            ProductName = firstItem?.ItemName ?? "—",
            AuctionId = firstItem?.AuctionId,
            CanMarkUnderReview = complaint.Status == ComplaintStatuses.Pending,
            CanApprove = isOpen && orderIsPaid && !hasApprovedForOrder,
            CanReject = isOpen,
            CanClose = complaint.Status is ComplaintStatuses.Approved or ComplaintStatuses.Rejected
        };
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(
        int complaintId,
        string action,
        int adminUserId,
        string? adminNotes,
        string? resolutionNote,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _dbContext.Complaints
            .Include(c => c.Order)
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.DeletedAt == null, cancellationToken);

        if (complaint is null)
        {
            return (false, "Complaint not found.");
        }

        var normalizedAction = action.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        switch (normalizedAction)
        {
            case ComplaintStatusActions.UnderReview:
                if (complaint.Status != ComplaintStatuses.Pending)
                {
                    return (false, "Only pending complaints can be marked under review.");
                }

                complaint.Status = ComplaintStatuses.UnderReview;
                break;

            case ComplaintStatusActions.Approve:
                if (complaint.Status is not (ComplaintStatuses.Pending or ComplaintStatuses.UnderReview))
                {
                    return (false, "Only pending or under-review complaints can be approved.");
                }

                if (string.IsNullOrWhiteSpace(resolutionNote))
                {
                    return (false, "Resolution note is required when approving a complaint.");
                }

                if (complaint.OrderId.HasValue)
                {
                    if (complaint.Order is null
                        || complaint.Order.Status is not (OrderStatuses.Paid or OrderStatuses.Shipped or OrderStatuses.Delivered))
                    {
                        return (false, "The linked order must be paid before approval.");
                    }

                    var duplicateApproved = await _dbContext.Complaints.AnyAsync(
                        c => c.DeletedAt == null
                             && c.OrderId == complaint.OrderId
                             && c.Id != complaint.Id
                             && c.Status == ComplaintStatuses.Approved,
                        cancellationToken);

                    if (duplicateApproved)
                    {
                        return (false, "Another approved complaint already exists for this order.");
                    }
                }

                complaint.Status = ComplaintStatuses.Approved;
                complaint.ResolutionNote = resolutionNote.Trim();
                complaint.ReviewedBy = adminUserId;
                complaint.ReviewedAt = now;
                break;

            case ComplaintStatusActions.Reject:
                if (complaint.Status is not (ComplaintStatuses.Pending or ComplaintStatuses.UnderReview))
                {
                    return (false, "Only pending or under-review complaints can be rejected.");
                }

                if (string.IsNullOrWhiteSpace(resolutionNote))
                {
                    return (false, "Resolution note is required when rejecting a complaint.");
                }

                complaint.Status = ComplaintStatuses.Rejected;
                complaint.ResolutionNote = resolutionNote.Trim();
                complaint.ReviewedBy = adminUserId;
                complaint.ReviewedAt = now;
                break;

            case ComplaintStatusActions.Close:
                if (complaint.Status is not (ComplaintStatuses.Approved or ComplaintStatuses.Rejected))
                {
                    return (false, "Only approved or rejected complaints can be closed.");
                }

                complaint.Status = ComplaintStatuses.Closed;
                break;

            case ComplaintStatusActions.AddNote:
                if (string.IsNullOrWhiteSpace(adminNotes))
                {
                    return (false, "Admin notes cannot be empty.");
                }

                complaint.AdminNotes = string.IsNullOrWhiteSpace(complaint.AdminNotes)
                    ? adminNotes.Trim()
                    : $"{complaint.AdminNotes.Trim()}\n\n{adminNotes.Trim()}";

                complaint.UpdatedAt = now;
                complaint.UpdatedBy = adminUserId;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return (true, "Admin notes saved.");

            default:
                return (false, "Unknown action.");
        }

        if (!string.IsNullOrWhiteSpace(adminNotes))
        {
            complaint.AdminNotes = string.IsNullOrWhiteSpace(complaint.AdminNotes)
                ? adminNotes.Trim()
                : $"{complaint.AdminNotes.Trim()}\n\n{adminNotes.Trim()}";
        }

        complaint.UpdatedAt = now;
        complaint.UpdatedBy = adminUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (normalizedAction is ComplaintStatusActions.Approve or ComplaintStatusActions.Reject)
        {
            await TryNotifyBuyerAsync(complaint, normalizedAction, cancellationToken);
        }

        var message = normalizedAction switch
        {
            ComplaintStatusActions.UnderReview => "Complaint marked as under review.",
            ComplaintStatusActions.Approve => "Complaint approved. Process the refund manually if PayPal refund API is not integrated.",
            ComplaintStatusActions.Reject => "Complaint rejected.",
            ComplaintStatusActions.Close => "Complaint closed.",
            _ => "Complaint updated."
        };

        return (true, message);
    }

    private async Task TryNotifyBuyerAsync(
        Complaint complaint,
        string action,
        CancellationToken cancellationToken)
    {
        try
        {
            if (action == ComplaintStatusActions.Approve)
            {
                await _notificationService.CreateAndPushAsync(
                    complaint.BuyerId,
                    "Refund request approved",
                    complaint.ResolutionNote ?? "Your refund request has been approved.",
                    NotificationType.Refund,
                    "/Refund/Confirmation?requestId=" + Uri.EscapeDataString(complaint.RequestReference),
                    NotificationReferenceTypes.RefundApproved,
                    complaint.Id,
                    cancellationToken: cancellationToken);
            }
            else if (action == ComplaintStatusActions.Reject)
            {
                await _notificationService.CreateAndPushAsync(
                    complaint.BuyerId,
                    "Refund request rejected",
                    complaint.ResolutionNote ?? "Your refund request has been rejected.",
                    NotificationType.Refund,
                    "/Refund",
                    NotificationReferenceTypes.RefundRejected,
                    complaint.Id,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify buyer about complaint {ComplaintId}", complaint.Id);
        }
    }

    private static IReadOnlyList<string> ParseEvidenceUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void NormalizeFilter(ComplaintFilterViewModel filter)
    {
        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        if (filter.PageSize is < 1 or > 100)
        {
            filter.PageSize = 10;
        }
    }

    private static (DateTime? StartDate, DateTime? EndDate) ParseDateRange(string? dateRange)
    {
        if (string.IsNullOrWhiteSpace(dateRange))
        {
            return (null, null);
        }

        var dates = dateRange.Split(" - ", StringSplitOptions.TrimEntries);

        if (dates.Length != 2)
        {
            return (null, null);
        }

        var isStartValid = DateTime.TryParseExact(
            dates[0],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var startDate);

        var isEndValid = DateTime.TryParseExact(
            dates[1],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var endDate);

        if (!isStartValid || !isEndValid)
        {
            return (null, null);
        }

        return (startDate.Date, endDate.Date.AddDays(1));
    }
}
