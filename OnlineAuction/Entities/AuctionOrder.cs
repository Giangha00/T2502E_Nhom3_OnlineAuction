namespace OnlineAuction.Entities;

public class AuctionOrder : AuditableEntity
{
    public int Id { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public int BuyerId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; } = 45.00m;

    public decimal VaultInsurance { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal SellerFee { get; set; }

    /// <summary>
    /// Phase 1 ledger: Subtotal − SellerFee when paid. Not a PayPal payout.
    /// </summary>
    public decimal SellerProceeds { get; set; }

    public decimal DepositApplied { get; set; }

    public string Status { get; set; } = OrderStatuses.PendingPayment;

    public string OrderSource { get; set; } = OrderSources.AuctionWin;

    public DateTime PaymentDeadline { get; set; }

    public string? ShippingFullName { get; set; }

    public string? ShippingAddress { get; set; }

    public string? ShippingCity { get; set; }

    public string? ShippingPhone { get; set; }

    public string? PaymentMethod { get; set; }

    public ApplicationUser Buyer { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}

public static class OrderStatuses
{
    public const string PendingPayment = "pending_payment";
    public const string Paid = "paid";
    public const string Shipped = "shipped";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";
}

public static class OrderSources
{
    public const string AuctionWin = "auction_win";
    public const string BuyNow = "buy_now";
}
