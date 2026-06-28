using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IRefundComplaintService
{
    Task<IReadOnlyList<RefundEligibleOrderViewModel>> GetEligibleOrdersAsync(
        int buyerId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, string? RequestReference)> SubmitAsync(
        int buyerId,
        RefundSubmitViewModel model,
        CancellationToken cancellationToken = default);

    Task<RefundConfirmationViewModel?> GetConfirmationAsync(
        int buyerId,
        string requestReference,
        CancellationToken cancellationToken = default);
}
