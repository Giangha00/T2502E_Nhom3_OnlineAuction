namespace OnlineAuction.Models;

public class WonOrderItem
{
    public int AuctionId { get; set; }
    public int OrderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal WinningBid { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal VaultInsurance { get; set; }
    public decimal DepositApplied { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PaymentDeadline { get; set; }
    public string OrderReference { get; set; } = string.Empty;
    public string OrderSource { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsSelectedByDefault { get; set; }
    public bool IsExpired => PaymentDeadline <= DateTime.UtcNow;
}

public class OrderPageViewModel
{
    public List<WonOrderItem> Items { get; set; } = [];
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? SelectedPaymentMethod { get; set; }
    public bool ShippingSaved { get; set; }
    public bool HasExpiredOrder => Items.Any(item => item.IsExpired);
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal VaultInsurance { get; set; }
    public decimal DepositApplied { get; set; }
    public decimal TotalAmount { get; set; }
    public int SelectedItemCount { get; set; }
    public List<PaymentMethodOption> PaymentMethods { get; set; } = [];
}
