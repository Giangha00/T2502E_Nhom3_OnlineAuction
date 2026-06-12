namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserListViewModel
{
    public List<UserListItemViewModel> Users { get; set; } = [];

    public UserFilterViewModel Filter { get; set; } = new();

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => Filter.Page > 1;

    public bool HasNextPage => Filter.Page < TotalPages;
}