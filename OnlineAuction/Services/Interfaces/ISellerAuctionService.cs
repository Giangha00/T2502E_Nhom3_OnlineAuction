using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface ISellerAuctionService
{
    Task<(bool Success, string Message)> CreateAsync(CreateAuctionViewModel model, int sellerId);
}
