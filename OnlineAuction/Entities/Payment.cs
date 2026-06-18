namespace OnlineAuction.Entities;

public class Payment : AuditableEntity
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = PaymentStatuses.Pending;

    public string? TransactionId { get; set; }

    public string? PayPalOrderId { get; set; }

    public DateTime? PaidAt { get; set; }

    public AuctionOrder Order { get; set; } = null!;
}

public static class PaymentStatuses
{
    public const string Pending = "pending";
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}
