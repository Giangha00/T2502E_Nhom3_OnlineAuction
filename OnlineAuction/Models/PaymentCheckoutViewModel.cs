namespace OnlineAuction.Models;

public class PaymentCheckoutViewModel
{
    public AuctionItemViewModel Auction { get; set; } = new();
    public string OrderReference { get; set; } = string.Empty;
    public DateTime PaymentDeadline { get; set; }
    public decimal WinningBid { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PaymentMethodOption> PaymentMethods { get; set; } = [];
}

public class PaymentMethodOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PaymentConfirmationViewModel
{
    public int OrderId { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public string AuctionName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTime PaidAt { get; set; }

    public List<PaymentConfirmationItem> Items { get; set; } = [];
}

public class PaymentConfirmationItem
{
    public string Name { get; set; } = string.Empty;

    public string Grade { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
