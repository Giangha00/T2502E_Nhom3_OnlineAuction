using OnlineAuction.Entities;
using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IAuctionService
{
    HomeViewModel GetHomePage();

    Task<HomeViewModel> GetHomePageAsync();

    Task<AuctionViewModel> GetAuctionIndexAsync(string listingType = ListingTypes.Auction);

    Task<AuctionViewModel> GetBuyNowIndexAsync();

    AuctionViewModel GetAuctionIndex(string listingType = ListingTypes.Auction);

    Task<ProductDetailViewModel?> GetProductDetailAsync(int id, int? currentUserId = null, bool isAdmin = false);

    Task<AuctionItemViewModel?> GetAuctionByIdAsync(int id);

    AuctionItemViewModel? GetAuctionById(int id);

    Task<IReadOnlyList<AuctionItemViewModel>> GetAllAuctionsAsync();

    IReadOnlyList<AuctionItemViewModel> GetAllAuctions();
}
