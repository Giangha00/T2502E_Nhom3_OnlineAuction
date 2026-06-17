using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface ISellerAuctionService
{
    Task<List<AuctionItemViewModel>> GetSellerAuctionsAsync(int sellerId, string? channel = null);

    Task<(bool Success, string Message, int? AuctionId)> CreateAsync(
        CreateAuctionViewModel model,
        int sellerId);

    Task<(bool Success, string Message, int? ProductId)> CreateBuyNowAsync(
        CreateBuyNowViewModel model,
        int sellerId);

    Task<SellerAuctionFormViewModel?> GetEditFormAsync(
        int auctionId,
        int sellerId);

    Task<(bool Success, string Message)> UpdateAsync(
        SellerAuctionFormViewModel model,
        int sellerId);

    Task<(bool Success, string Message)> CancelAsync(
        int auctionId,
        int sellerId);
}
