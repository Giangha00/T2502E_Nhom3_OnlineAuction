using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class ComplaintFlowTests
{
    [Fact]
    public async Task SubmitAsync_CreatesPendingComplaintForEligiblePaidOrder()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        var notifications = new RecordingNotificationService();
        var service = CreateRefundService(db, notifications);

        var result = await service.SubmitAsync(1, ValidSubmitModel(orderId: 10));

        Assert.True(result.Success);
        Assert.NotNull(result.RequestReference);

        var complaint = await db.Complaints.SingleAsync();
        Assert.Equal(ComplaintStatuses.Pending, complaint.Status);
        Assert.Equal(result.RequestReference, complaint.RequestReference);
        Assert.StartsWith("RF-", complaint.RequestReference);
        Assert.Equal(ComplaintReasonCodes.Damaged, complaint.ReasonCode);
        Assert.Equal(1, notifications.CallCount);
        Assert.Equal(NotificationReferenceTypes.RefundRequested, notifications.LastReferenceType);
    }

    [Fact]
    public async Task SubmitAsync_RejectsDuplicateOpenComplaintForSameOrder()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.UnderReview));
        await db.SaveChangesAsync();

        var service = CreateRefundService(db);
        var result = await service.SubmitAsync(1, ValidSubmitModel(orderId: 10));

        Assert.False(result.Success);
        Assert.Equal("A refund request for this order is already pending review.", result.Message);
        Assert.Equal(1, await db.Complaints.CountAsync());
    }

    [Fact]
    public async Task SubmitAsync_RejectsIneligibleOrderStatus()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.PendingPayment);

        var service = CreateRefundService(db);
        var result = await service.SubmitAsync(1, ValidSubmitModel(orderId: 10));

        Assert.False(result.Success);
        Assert.Equal("Refund requests are only available for paid orders.", result.Message);
    }

    [Fact]
    public async Task SubmitAsync_RejectsExpiredRefundWindow()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(
            db,
            orderStatus: OrderStatuses.Delivered,
            updatedAt: DateTime.UtcNow.AddDays(-20),
            paidAt: DateTime.UtcNow.AddDays(-25));

        var service = CreateRefundService(db);
        var result = await service.SubmitAsync(1, ValidSubmitModel(orderId: 10));

        Assert.False(result.Success);
        Assert.Equal("Refund requests must be submitted within 14 days of delivery or expected delivery.", result.Message);
    }

    [Fact]
    public async Task GetEligibleOrdersAsync_ExcludesOrdersWithOpenComplaint()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.Pending));
        await db.SaveChangesAsync();

        var service = CreateRefundService(db);
        var orders = await service.GetEligibleOrdersAsync(1);

        Assert.Empty(orders);
    }

    [Fact]
    public async Task UpdateStatusAsync_ApproveRequiresEligibleLinkedOrderAndResolutionNote()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        db.Users.Add(CreateAdmin());
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.Pending));
        await db.SaveChangesAsync();

        var notifications = new RecordingNotificationService();
        var service = CreateAdminService(db, notifications);
        var result = await service.UpdateStatusAsync(
            100,
            ComplaintStatusActions.Approve,
            2,
            "Checked order and evidence.",
            "Refund approved after review.");

        Assert.True(result.Success);

        var complaint = await db.Complaints.SingleAsync(c => c.Id == 100);
        Assert.Equal(ComplaintStatuses.Approved, complaint.Status);
        Assert.Equal(2, complaint.ReviewedBy);
        Assert.NotNull(complaint.ReviewedAt);
        Assert.Equal("Checked order and evidence.", complaint.AdminNotes);
        Assert.Equal("Refund approved after review.", complaint.ResolutionNote);
        Assert.Equal(NotificationReferenceTypes.RefundApproved, notifications.LastReferenceType);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectThenCloseCompletesComplaint()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        db.Users.Add(CreateAdmin());
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.UnderReview));
        await db.SaveChangesAsync();

        var service = CreateAdminService(db);
        var rejectResult = await service.UpdateStatusAsync(
            100,
            ComplaintStatusActions.Reject,
            2,
            null,
            "Evidence does not support the request.");
        var closeResult = await service.UpdateStatusAsync(
            100,
            ComplaintStatusActions.Close,
            2,
            null,
            null);

        Assert.True(rejectResult.Success);
        Assert.True(closeResult.Success);

        var complaint = await db.Complaints.SingleAsync(c => c.Id == 100);
        Assert.Equal(ComplaintStatuses.Closed, complaint.Status);
        Assert.Equal("Evidence does not support the request.", complaint.ResolutionNote);
    }

    [Fact]
    public async Task UpdateStatusAsync_AddNoteAppendsInternalNotes()
    {
        await using var db = CreateDbContext();
        db.Users.Add(CreateBuyer());
        db.Users.Add(CreateAdmin());
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.Pending, orderId: null, adminNotes: "First note"));
        await db.SaveChangesAsync();

        var service = CreateAdminService(db);
        var result = await service.UpdateStatusAsync(
            100,
            ComplaintStatusActions.AddNote,
            2,
            "Second note",
            null);

        Assert.True(result.Success);

        var complaint = await db.Complaints.SingleAsync(c => c.Id == 100);
        Assert.Equal("First note\n\nSecond note", complaint.AdminNotes);
        Assert.Equal(2, complaint.UpdatedBy);
    }

    [Fact]
    public async Task GetComplaintDetailAsync_DoesNotThrowWhenOrderItemContextIsMissing()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid, includeOrderItem: true);
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.Pending));
        await db.SaveChangesAsync();

        var service = CreateAdminService(db);
        var detail = await service.GetComplaintDetailAsync(100);

        Assert.NotNull(detail);
        Assert.Equal("Not available", detail.ProductName);
        Assert.Null(detail.SellerId);
        Assert.True(detail.CanApprove);
    }

    [Fact]
    public async Task GetComplaintDetailAsync_ShowsApprovalBlockedWhenAnotherComplaintApproved()
    {
        await using var db = CreateDbContext();
        await SeedBuyerWithOrderAsync(db, orderStatus: OrderStatuses.Paid);
        db.Complaints.Add(CreateComplaint(100, ComplaintStatuses.Pending));
        db.Complaints.Add(CreateComplaint(101, ComplaintStatuses.Approved));
        await db.SaveChangesAsync();

        var service = CreateAdminService(db);
        var detail = await service.GetComplaintDetailAsync(100);

        Assert.NotNull(detail);
        Assert.True(detail.HasApprovedComplaintForOrder);
        Assert.False(detail.CanApprove);
        Assert.True(detail.CanReject);
    }

    private static AuctionHouseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuctionHouseDbContext(options);
    }

    private static RefundComplaintService CreateRefundService(
        AuctionHouseDbContext db,
        RecordingNotificationService? notifications = null) =>
        new(
            db,
            notifications ?? new RecordingNotificationService(),
            new EchoNotificationLocalizer(),
            NullLogger<RefundComplaintService>.Instance);

    private static AdminComplaintService CreateAdminService(
        AuctionHouseDbContext db,
        RecordingNotificationService? notifications = null) =>
        new(
            db,
            notifications ?? new RecordingNotificationService(),
            new EchoNotificationLocalizer(),
            NullLogger<AdminComplaintService>.Instance);

    private static async Task SeedBuyerWithOrderAsync(
        AuctionHouseDbContext db,
        string orderStatus,
        DateTime? updatedAt = null,
        DateTime? paidAt = null,
        bool includeOrderItem = false)
    {
        var buyer = CreateBuyer();
        var order = new AuctionOrder
        {
            Id = 10,
            BuyerId = buyer.Id,
            Buyer = buyer,
            OrderReference = "ORD-100",
            Status = orderStatus,
            Subtotal = 100m,
            TotalAmount = 120m,
            PaymentDeadline = DateTime.UtcNow.AddDays(1),
            CreatedAt = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAt ?? DateTime.UtcNow.AddDays(-1),
            PaymentMethod = "PayPal"
        };

        order.Payments.Add(new Payment
        {
            Id = 20,
            OrderId = order.Id,
            Order = order,
            Amount = 120m,
            Status = PaymentStatuses.Success,
            PaidAt = paidAt ?? DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        if (includeOrderItem)
        {
            var orderItem = new OrderItem
            {
                Id = 30,
                OrderId = order.Id,
                Order = order,
                AuctionId = 999,
                ItemName = "Vintage card",
                WinningBid = 100m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            order.Items.Add(orderItem);
            db.OrderItems.Add(orderItem);
        }

        db.Users.Add(buyer);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    private static RefundSubmitViewModel ValidSubmitModel(int? orderId = 10) =>
        new()
        {
            OrderId = orderId,
            ContactName = "Buyer One",
            ContactEmail = "buyer@example.com",
            ReasonCode = ComplaintReasonCodes.Damaged,
            Description = "The item arrived with visible damage on the front side.",
            RequestedAmount = 50m,
            EvidenceUrls = "https://example.com/photo-one.jpg\nhttps://example.com/photo-two.jpg"
        };

    private static Complaint CreateComplaint(
        int id,
        string status,
        int? orderId = 10,
        string? adminNotes = null) =>
        new()
        {
            Id = id,
            RequestReference = $"RF-20260720-{id}",
            OrderId = orderId,
            OrderReference = orderId.HasValue ? "ORD-100" : null,
            BuyerId = 1,
            ComplaintType = ComplaintTypes.Refund,
            ReasonCode = ComplaintReasonCodes.Damaged,
            Description = "The item arrived with visible damage on the front side.",
            RequestedAmount = 50m,
            ContactName = "Buyer One",
            ContactEmail = "buyer@example.com",
            Status = status,
            AdminNotes = adminNotes,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

    private static ApplicationUser CreateBuyer() =>
        new()
        {
            Id = 1,
            UserName = "buyer@example.com",
            NormalizedUserName = "BUYER@EXAMPLE.COM",
            Email = "buyer@example.com",
            NormalizedEmail = "BUYER@EXAMPLE.COM",
            PhoneNumber = string.Empty,
            FullName = "Buyer One"
        };

    private static ApplicationUser CreateAdmin() =>
        new()
        {
            Id = 2,
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            PhoneNumber = string.Empty,
            FullName = "Admin One",
            IsSuperAdmin = true
        };

    private sealed class RecordingNotificationService : INotificationService
    {
        public int CallCount { get; private set; }
        public string? LastReferenceType { get; private set; }

        public Task<NotificationItemViewModel?> CreateAndPushAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            string? relatedUrl,
            string? referenceType = null,
            int? referenceId = null,
            TimeSpan? debounceWindow = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastReferenceType = referenceType;
            return Task.FromResult<NotificationItemViewModel?>(new NotificationItemViewModel
            {
                Id = CallCount,
                Title = title,
                Message = message,
                Type = type,
                RelatedUrl = relatedUrl
            });
        }

        public Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
            int userId,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationItemViewModel>>([]);

        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> MarkAsReadAsync(
            int userId,
            int notificationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RegisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            string? deviceInfo,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnregisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ProcessAuctionEndingSoonNotificationsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ProcessAuctionStartingSoonNotificationsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EchoNotificationLocalizer : INotificationLocalizer
    {
        public string this[string name] => name;

        public string Format(string name, params object[] args) =>
            args.Length == 0 ? name : string.Format("{0}: {1}", name, string.Join(", ", args));
    }
}
