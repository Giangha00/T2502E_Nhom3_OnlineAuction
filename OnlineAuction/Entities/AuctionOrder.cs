namespace OnlineAuction.Entities;

public class AuctionOrder
{
    public int Id { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public int BuyerId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; } = 45.00m;

    public decimal VaultInsurance { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = OrderStatuses.PendingPayment;

    public DateTime PaymentDeadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
