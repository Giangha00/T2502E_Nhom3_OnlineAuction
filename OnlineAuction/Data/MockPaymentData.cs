using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockPaymentData
{
    public static List<SavedPaymentMethodViewModel> GetSavedPaymentMethods() =>
    [
        new()
        {
            Id = "card-visa-4567",
            CardType = "visa",
            MaskedNumber = "**** **** **** 4567",
            LastFour = "4567",
            HolderName = "NGUYEN VAN A",
            ExpiryMonth = "12",
            ExpiryYear = "28",
            BillingAddress = "123 Nguyen Hue Street\nHo Chi Minh City\nVietnam",
            IsDefault = true
        },
        new()
        {
            Id = "card-mc-8901",
            CardType = "mastercard",
            MaskedNumber = "**** **** **** 8901",
            LastFour = "8901",
            HolderName = "NGUYEN VAN A",
            ExpiryMonth = "06",
            ExpiryYear = "27",
            BillingAddress = "45 Le Loi Boulevard\nDa Nang\nVietnam",
            IsDefault = false
        }
    ];
}
