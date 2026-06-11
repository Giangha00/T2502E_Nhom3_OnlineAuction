namespace OnlineAuction.Models;

public class CartViewModel
{
    public List<CartItemViewModel> WatchingItems { get; set; } = [];
    public List<CartItemViewModel> WonItems { get; set; } = [];
    public List<AuctionItemViewModel> AllAuctions { get; set; } = [];
    public int WatchingCount => WatchingItems.Count;
    public int WonCount => WonItems.Count;
    public int TotalItemCount => WatchingCount + WonCount;
    public decimal TotalPendingPayment { get; set; }
}

public class CartItemViewModel
{
    public AuctionItemViewModel Auction { get; set; } = new();
    public DateTime? PaymentDeadline { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalDue { get; set; }
}
