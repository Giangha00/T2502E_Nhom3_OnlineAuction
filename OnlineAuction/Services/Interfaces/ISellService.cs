using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface ISellService
{
    CreateAuctionViewModel BuildCreateForm();

    void PopulateOptions(CreateAuctionViewModel model);

    IEnumerable<(string Key, string Message)> ValidateCreateAuction(CreateAuctionViewModel model);
}
