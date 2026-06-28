using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public static class ComplaintDisplayHelper
{
    public static string FormatRequestReference(int id, DateTime createdAt) =>
        Complaint.BuildRequestReference(id, createdAt);

    public static string GetStatusLabel(string status) => status switch
    {
        ComplaintStatuses.Pending => "Pending",
        ComplaintStatuses.UnderReview => "Under review",
        ComplaintStatuses.Approved => "Approved",
        ComplaintStatuses.Rejected => "Rejected",
        ComplaintStatuses.Closed => "Closed",
        _ => status
    };

    public static string GetTypeLabel(string type) => type switch
    {
        ComplaintTypes.Refund => "Refund",
        ComplaintTypes.Dispute => "Dispute",
        ComplaintTypes.Authenticity => "Authenticity",
        ComplaintTypes.Other => "Other",
        _ => type
    };

    public static string GetReasonLabel(string reasonCode) =>
        ComplaintReasonCodes.Labels.TryGetValue(reasonCode, out var label) ? label : reasonCode;

    public static string GetStatusBadgeClass(string status) => status switch
    {
        ComplaintStatuses.Pending => "bg-warning-50 text-warning-700 border-warning-200",
        ComplaintStatuses.UnderReview => "bg-brand-50 text-brand-700 border-brand-200",
        ComplaintStatuses.Approved => "bg-success-50 text-success-700 border-success-200",
        ComplaintStatuses.Rejected => "bg-error-50 text-error-700 border-error-200",
        ComplaintStatuses.Closed => "bg-gray-100 text-gray-600 border-gray-200",
        _ => "bg-gray-100 text-gray-600 border-gray-200"
    };
}
