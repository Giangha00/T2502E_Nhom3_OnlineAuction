using OnlineAuction.Areas.Admin.ViewModels.Complaints;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminComplaintService
{
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

    Task<ComplaintListViewModel> GetComplaintsAsync(
        ComplaintFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<ComplaintDetailViewModel?> GetComplaintDetailAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> UpdateStatusAsync(
        int complaintId,
        string action,
        int adminUserId,
        string? adminNotes,
        string? resolutionNote,
        CancellationToken cancellationToken = default);
}
