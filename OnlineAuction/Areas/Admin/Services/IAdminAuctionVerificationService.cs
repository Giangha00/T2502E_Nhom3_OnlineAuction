using OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminAuctionVerificationService
{
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

    Task<AuctionVerificationListViewModel> GetPendingVerificationsAsync(
        AuctionVerificationFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<AuctionVerificationDetailViewModel?> GetVerificationDetailAsync(
        int auctionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ApproveAsync(
        int auctionId,
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> RejectAsync(
        int auctionId,
        int adminUserId,
        string rejectReason,
        CancellationToken cancellationToken = default);

    Task<int> ActivateScheduledAuctionsAsync(CancellationToken cancellationToken = default);
}
