using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface ISellService
{
    CreateAuctionViewModel BuildCreateForm();

    CreateBuyNowViewModel BuildBuyNowForm();

    void PopulateOptions(CreateAuctionViewModel model);

    void PopulateOptions(CreateBuyNowViewModel model);

    IEnumerable<(string Key, string Message)> ValidateCreateAuction(CreateAuctionViewModel model);

    IEnumerable<(string Key, string Message)> ValidateCreateBuyNow(CreateBuyNowViewModel model);
}
