using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IPaymentService
{
    PaymentInformationViewModel GetPaymentInformation();

    PaymentCheckoutViewModel? BuildCheckout(int? auctionId);

    PaymentConfirmationViewModel BuildConfirmation(
        string? orderRef,
        string? auctionName,
        decimal? total,
        string? method);
}
