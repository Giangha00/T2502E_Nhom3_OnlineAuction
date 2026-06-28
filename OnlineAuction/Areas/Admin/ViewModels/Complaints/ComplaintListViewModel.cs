namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public class ComplaintListViewModel
{
    public IReadOnlyList<ComplaintListItemViewModel> Items { get; init; } = [];

    public ComplaintFilterViewModel Filter { get; init; } = new();

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}
