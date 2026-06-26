namespace OnlineAuction.Entities;

public class AuctionRegistrationDeposit : AuditableEntity
{
    public long Id { get; set; }

    // Phiên đấu giá nào
    public int AuctionId { get; set; }

    // User nào đặt cọc
    public int UserId { get; set; }

    // Liên kết với bản ghi đăng ký đấu giá
    public long AuctionRegistrationId { get; set; }

    // Số tiền cọc được lưu cố định tại thời điểm initiate
    public decimal Amount { get; set; }

    // pending, paid, cancelled, refunded, failed
    public string Status { get; set; } = AuctionRegistrationDepositStatuses.Pending;

    // PayPal order id nhận được khi tạo checkout order
    public string? PayPalOrderId { get; set; }

    // PayPal capture id nhận được sau khi capture thành công
    public string? PayPalCaptureId { get; set; }

    // PayPal refund id sau khi hoàn tiền
    public string? PayPalRefundId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public Auction Auction { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public AuctionRegistration Registration { get; set; } = null!;
}

public static class AuctionRegistrationDepositStatuses
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";
    public const string Failed = "failed";
    // Tiền cọc của người thắng đã được dùng để trừ vào số tiền cần thanh toán của order.
    // Ví dụ: thắng bid 500$, đã cọc 50$ thì order chỉ cần trả phần còn lại.
    // Trạng thái này KHÔNG phải refund, vì tiền không trả lại qua PayPal.
    public const string Applied = "applied";
}