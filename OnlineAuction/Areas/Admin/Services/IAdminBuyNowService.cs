using OnlineAuction.Areas.Admin.ViewModels.BuyNow;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminBuyNowService
{
    Task<BuyNowListViewModel> GetListingsAsync(BuyNowFilterViewModel filter);

    Task<BuyNowDetailViewModel?> GetDetailsAsync(int id);

    Task<BuyNowFormViewModel> BuildCreateFormAsync();

    Task<BuyNowFormViewModel?> GetEditFormAsync(int id);

    Task PopulateFormOptionsAsync(BuyNowFormViewModel model);

    Task<(bool Success, string Message)> CreateAsync(BuyNowFormViewModel model);

    Task<(bool Success, string Message)> UpdateAsync(BuyNowFormViewModel model);

    Task<(bool Success, string Message)> CancelAsync(int id);
}
