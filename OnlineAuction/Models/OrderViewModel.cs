namespace OnlineAuction.Models;

public class WonOrderItem
{
    public int AuctionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal WinningBid { get; set; }
    public DateTime PaymentDeadline { get; set; }
    public string OrderReference { get; set; } = string.Empty;
}

public class OrderPageViewModel
{
    public List<WonOrderItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal VaultInsurance { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PaymentMethodOption> PaymentMethods { get; set; } = [];
}
