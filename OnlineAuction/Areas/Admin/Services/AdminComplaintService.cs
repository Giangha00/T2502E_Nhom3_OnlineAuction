using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Complaints;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminComplaintService : IAdminComplaintService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<AdminComplaintService> _logger;

    public AdminComplaintService(
        AuctionHouseDbContext dbContext,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        ILogger<AdminComplaintService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _logger = logger;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Complaints.AsNoTracking()
            .CountAsync(
                complaint => complaint.DeletedAt == null
                             && ComplaintStatuses.OpenStatuses.Contains(complaint.Status),
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
                || complaint.ContactName.Contains(keyword)
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
                    ? complaint.Order.Items
                        .OrderBy(item => item.Id)
                        .Select(item => item.ItemName != "" ? item.ItemName : item.Auction.Product.Name)
                        .FirstOrDefault() ?? "—"
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

        await BackfillMissingProductNamesAsync(items, cancellationToken);

        return new ComplaintListViewModel
        {
            Items = items,
            Filter = filter,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    private async Task BackfillMissingProductNamesAsync(
        IReadOnlyList<ComplaintListItemViewModel> items,
        CancellationToken cancellationToken)
    {
        var missing = items
            .Where(item => string.IsNullOrWhiteSpace(item.ProductName) || item.ProductName == "—")
            .Where(item => !string.IsNullOrWhiteSpace(item.OrderReference))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var item in missing)
        {
            var resolved = await ResolveComplaintProductAsync(
                order: null,
                firstItem: null,
                orderReference: item.OrderReference,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(resolved.ProductName) && item.Id > 0)
            {
                // List items don't include OrderId; recover via complaint → order notifications.
                var orderId = await _dbContext.Complaints
                    .AsNoTracking()
                    .Where(c => c.Id == item.Id)
                    .Select(c => c.OrderId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (orderId.HasValue)
                {
                    resolved = await TryResolveProductFromOrderNotificationsAsync(
                        orderId.Value,
                        item.OrderReference,
                        cancellationToken) ?? resolved;
                }
            }

            if (!string.IsNullOrWhiteSpace(resolved.ProductName))
            {
                item.ProductName = resolved.ProductName;
            }
        }
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

        var firstItem = complaint.Order?.Items
            .Where(item => item.DeletedAt == null)
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        var resolvedProduct = await ResolveComplaintProductAsync(
            complaint.Order,
            firstItem,
            complaint.OrderReference ?? complaint.Order?.OrderReference,
            cancellationToken);

        var seller = resolvedProduct.Seller
                     ?? firstItem?.Auction?.Product?.Seller;
        var successfulPayment = complaint.Order?.Payments
            .Where(payment => payment.DeletedAt == null && payment.Status == PaymentStatuses.Success)
            .OrderByDescending(payment => payment.PaidAt ?? payment.UpdatedAt ?? payment.CreatedAt)
            .FirstOrDefault();

        var productName = string.IsNullOrWhiteSpace(resolvedProduct.ProductName)
            ? "—"
            : resolvedProduct.ProductName;

        // Prefer payment.PaidAt; if payment rows were removed but order is still marked paid, fall back.
        DateTime? paidAt = successfulPayment?.PaidAt
                           ?? successfulPayment?.UpdatedAt
                           ?? successfulPayment?.CreatedAt;
        if (!paidAt.HasValue
            && complaint.Order is { Status: OrderStatuses.Paid or OrderStatuses.Shipped or OrderStatuses.Delivered })
        {
            paidAt = complaint.Order.UpdatedAt ?? complaint.Order.CreatedAt;
        }

        var hasApprovedForOrder = complaint.OrderId.HasValue && await _dbContext.Complaints
            .AsNoTracking()
            .AnyAsync(
                c => c.DeletedAt == null
                     && c.OrderId == complaint.OrderId
                     && c.Id != complaint.Id
                     && c.Status == ComplaintStatuses.Approved,
                cancellationToken);

        var orderIsRefundEligible = complaint.Order != null
                                    && complaint.Order.Status is OrderStatuses.Paid
                                        or OrderStatuses.Shipped
                                        or OrderStatuses.Delivered;

        var isOpen = complaint.Status is ComplaintStatuses.Pending or ComplaintStatuses.UnderReview;
        var orderRefundEligibilityWarning = complaint.Order switch
        {
            null => "This complaint is not linked to a valid order and cannot be approved.",
            { Status: not (OrderStatuses.Paid or OrderStatuses.Shipped or OrderStatuses.Delivered) } =>
                "The linked order is not paid, shipped, or delivered yet, so refund approval is blocked.",
            _ => null
        };

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
            PaidAt = paidAt,
            BuyerId = complaint.BuyerId,
            BuyerName = complaint.Buyer.FullName,
            BuyerEmail = complaint.ContactEmail,
            SellerId = seller?.Id,
            SellerName = seller?.FullName,
            SellerEmail = seller?.Email,
            ProductName = productName,
            AuctionId = resolvedProduct.AuctionId ?? firstItem?.AuctionId,
            HasApprovedComplaintForOrder = hasApprovedForOrder,
            OrderRefundEligibilityWarning = orderRefundEligibilityWarning,
            CanMarkUnderReview = complaint.Status == ComplaintStatuses.Pending,
            CanApprove = isOpen && orderIsRefundEligible && !hasApprovedForOrder,
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

                if (!complaint.OrderId.HasValue || complaint.Order is null)
                {
                    return (false, "A valid linked order is required before approval.");
                }

                if (complaint.Order.Status is not (OrderStatuses.Paid or OrderStatuses.Shipped or OrderStatuses.Delivered))
                {
                    return (false, "The linked order must be paid, shipped, or delivered before approval.");
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

        if (normalizedAction is ComplaintStatusActions.Approve
            or ComplaintStatusActions.Reject
            or ComplaintStatusActions.UnderReview
            or ComplaintStatusActions.Close)
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
                    _notifyLocalizer[NotificationKeys.RefundApprovedTitle],
                    complaint.ResolutionNote ?? _notifyLocalizer[NotificationKeys.RefundApprovedMessage],
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
                    _notifyLocalizer[NotificationKeys.RefundRejectedTitle],
                    complaint.ResolutionNote ?? _notifyLocalizer[NotificationKeys.RefundRejectedMessage],
                    NotificationType.Refund,
                    "/Refund",
                    NotificationReferenceTypes.RefundRejected,
                    complaint.Id,
                    cancellationToken: cancellationToken);
            }
            else if (action == ComplaintStatusActions.UnderReview)
            {
                await _notificationService.CreateAndPushAsync(
                    complaint.BuyerId,
                    _notifyLocalizer[NotificationKeys.RefundUnderReviewTitle],
                    _notifyLocalizer[NotificationKeys.RefundUnderReviewMessage],
                    NotificationType.Refund,
                    "/Refund",
                    NotificationReferenceTypes.RefundUnderReview,
                    complaint.Id,
                    cancellationToken: cancellationToken);
            }
            else if (action == ComplaintStatusActions.Close)
            {
                await _notificationService.CreateAndPushAsync(
                    complaint.BuyerId,
                    _notifyLocalizer[NotificationKeys.RefundClosedTitle],
                    complaint.ResolutionNote ?? _notifyLocalizer[NotificationKeys.RefundClosedMessage],
                    NotificationType.Refund,
                    "/Refund",
                    NotificationReferenceTypes.RefundClosed,
                    complaint.Id,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify buyer about complaint {ComplaintId}", complaint.Id);
        }
    }

    private async Task<ResolvedComplaintProduct> ResolveComplaintProductAsync(
        AuctionOrder? order,
        OrderItem? firstItem,
        string? orderReference,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(firstItem?.ItemName))
        {
            return new ResolvedComplaintProduct(
                firstItem.ItemName.Trim(),
                firstItem.AuctionId,
                firstItem.Auction?.Product?.Seller);
        }

        if (!string.IsNullOrWhiteSpace(firstItem?.Auction?.Product?.Name))
        {
            return new ResolvedComplaintProduct(
                firstItem.Auction.Product.Name.Trim(),
                firstItem.AuctionId,
                firstItem.Auction.Product.Seller);
        }

        var auctionId = TryParseAuctionIdFromOrderReference(orderReference);
        if (auctionId.HasValue)
        {
            var auction = await _dbContext.Auctions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(a => a.Product)
                    .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(a => a.Id == auctionId.Value, cancellationToken);

            if (auction?.Product is not null && !string.IsNullOrWhiteSpace(auction.Product.Name))
            {
                return new ResolvedComplaintProduct(
                    auction.Product.Name.Trim(),
                    auction.Id,
                    auction.Product.Seller);
            }

            // Auction row may have been hard-deleted; try matching a live product by reconstructed name from notifications next.
        }

        if (order?.Id > 0)
        {
            var fromNotifications = await TryResolveProductFromOrderNotificationsAsync(
                order.Id,
                orderReference,
                cancellationToken);
            if (fromNotifications is not null)
            {
                return fromNotifications;
            }
        }

        return ResolvedComplaintProduct.Empty;
    }

    private async Task<ResolvedComplaintProduct?> TryResolveProductFromOrderNotificationsAsync(
        int orderId,
        string? orderReference,
        CancellationToken cancellationToken)
    {
        var messages = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n =>
                n.ReferenceId == orderId
                && (n.ReferenceType == NotificationReferenceTypes.SellerAwaitingPayment
                    || n.ReferenceType == NotificationReferenceTypes.SellerPaymentReceived
                    || n.ReferenceType == NotificationReferenceTypes.BuyNowOrderCreated
                    || n.ReferenceType == NotificationReferenceTypes.PaymentSuccess))
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => n.Message)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            var productName = TryExtractProductNameFromNotification(message, orderReference);
            if (string.IsNullOrWhiteSpace(productName))
            {
                continue;
            }

            var product = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Seller)
                .Where(p => p.DeletedAt == null && p.Name == productName)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var auctionId = await _dbContext.Auctions
                .AsNoTracking()
                .Where(a => a.DeletedAt == null && a.Product.Name == productName)
                .OrderByDescending(a => a.Id)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return new ResolvedComplaintProduct(productName, auctionId, product?.Seller);
        }

        return null;
    }

    private static string? TryExtractProductNameFromNotification(string? message, string? orderReference)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var text = message.Trim();

        // EN: Payment for {product} on order {ref} was confirmed via {method}.
        var paymentMatch = Regex.Match(
            text,
            @"^Payment for (.+) on order\s+\S+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (paymentMatch.Success)
        {
            return CleanExtractedProductName(paymentMatch.Groups[1].Value);
        }

        // EN: {product} has a winning buyer. Order {ref} is awaiting payment.
        var awaitingMatch = Regex.Match(
            text,
            @"^(.+) has a winning buyer\.\s*Order\s+\S+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (awaitingMatch.Success)
        {
            return CleanExtractedProductName(awaitingMatch.Groups[1].Value);
        }

        // VI: Thanh toán cho {product} ở đơn {ref} đã được xác nhận qua {method}.
        var viPaymentMatch = Regex.Match(
            text,
            @"^Thanh toán cho (.+) ở đơn\s+\S+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (viPaymentMatch.Success)
        {
            return CleanExtractedProductName(viPaymentMatch.Groups[1].Value);
        }

        // VI: {product} đã có người mua thắng. Đơn {ref} đang chờ thanh toán.
        var viAwaitingMatch = Regex.Match(
            text,
            @"^(.+) đã có người mua thắng\.\s*Đơn\s+\S+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (viAwaitingMatch.Success)
        {
            return CleanExtractedProductName(viAwaitingMatch.Groups[1].Value);
        }

        if (!string.IsNullOrWhiteSpace(orderReference))
        {
            var idx = text.IndexOf(orderReference, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                // Fallback: take text before the order reference, then trim common glue words.
                var before = text[..idx]
                    .Replace("Payment for ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("Thanh toán cho ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace(" on order ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace(" ở đơn ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("has a winning buyer.", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("đã có người mua thắng.", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim(' ', '-', '·', ',', '.', ':');

                if (!string.IsNullOrWhiteSpace(before) && before.Length is >= 3 and <= 200)
                {
                    return before;
                }
            }
        }

        return null;
    }

    private static string CleanExtractedProductName(string value) =>
        value.Trim().Trim('"', '\'', '“', '”');

    private static int? TryParseAuctionIdFromOrderReference(string? orderReference)
    {
        if (string.IsNullOrWhiteSpace(orderReference))
        {
            return null;
        }

        // BN-yyyyMMdd-{auctionId} or AH-yyyyMMdd-{auctionId}[+suffix]
        var match = Regex.Match(
            orderReference.Trim(),
            @"^(?:BN|AH)-\d{8}-(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success && int.TryParse(match.Groups[1].Value, out var auctionId)
            ? auctionId
            : null;
    }

    private sealed record ResolvedComplaintProduct(
        string? ProductName,
        int? AuctionId,
        ApplicationUser? Seller)
    {
        public static ResolvedComplaintProduct Empty { get; } = new(null, null, null);
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
