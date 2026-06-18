using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IOrderService
{
    OrderPageViewModel BuildOrderPage(ISession session);

    (bool Success, string OrderRef, string AuctionName, decimal Total, string Method) CompleteOrder(
        ISession session,
        string paymentMethod);
}
