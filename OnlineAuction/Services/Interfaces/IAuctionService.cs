using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IAuctionService
{
    HomeViewModel GetHomePage();

    AuctionViewModel GetAuctionIndex();

    Task<ProductDetailViewModel?> GetProductDetailAsync(int id);

    AuctionItemViewModel? GetAuctionById(int id);

    IReadOnlyList<AuctionItemViewModel> GetAllAuctions();
}
