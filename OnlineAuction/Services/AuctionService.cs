using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class AuctionService : IAuctionService
{
    public HomeViewModel GetHomePage()
    {
        var allAuctions = MockAuctionData.GetAllAuctions();
        var endingSoon = allAuctions.Where(a => a.Status == "Ending Soon").ToList();

        return new HomeViewModel
        {
            HotAuctions = MockAuctionData.GetHotAuctions(),
            FeaturedAuctions = MockAuctionData.GetFeaturedAuctions(),
            EndingSoonAuctions = endingSoon,
            WonAuctions = MockAuctionData.GetWonAuctions(),
            BestSellers = MockAuctionData.GetBestSellers(),
            Categories = MockAuctionData.GetCategories(),
            VaultPosts = MockAuctionData.GetVaultPosts(),
            TotalLiveAuctions = allAuctions.Count,
            EndingSoonCount = endingSoon.Count
        };
    }

    public AuctionViewModel GetAuctionIndex()
    {
        return new AuctionViewModel
        {
            Categories = MockAuctionData.GetCategories(),
            Auctions = MockAuctionData.GetAllAuctions()
        };
    }

    public ProductDetailViewModel? GetProductDetail(int id) =>
        MockProductDetailData.GetById(id);

    public AuctionItemViewModel? GetAuctionById(int id) =>
        MockAuctionData.GetAuctionById(id);

    public IReadOnlyList<AuctionItemViewModel> GetAllAuctions() =>
        MockAuctionData.GetAllAuctions();
}
