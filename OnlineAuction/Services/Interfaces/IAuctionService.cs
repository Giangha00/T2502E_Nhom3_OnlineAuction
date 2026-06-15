using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IAuctionService
{
    HomeViewModel GetHomePage();

    AuctionViewModel GetAuctionIndex();

    ProductDetailViewModel? GetProductDetail(int id);

    AuctionItemViewModel? GetAuctionById(int id);

    IReadOnlyList<AuctionItemViewModel> GetAllAuctions();
}
