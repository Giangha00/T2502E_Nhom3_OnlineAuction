namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public class ComplaintFilterViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? ReasonCode { get; set; }

    public string? ComplaintType { get; set; }

    public string? DateRange { get; set; }

    public string? SortOrder { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
