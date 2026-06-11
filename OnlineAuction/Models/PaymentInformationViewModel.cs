namespace OnlineAuction.Models;

public class PaymentInformationViewModel
{
    public List<SavedPaymentMethodViewModel> SavedMethods { get; set; } = [];
}

public class SavedPaymentMethodViewModel
{
    public string Id { get; set; } = string.Empty;
    public string CardType { get; set; } = "visa";
    public string MaskedNumber { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
