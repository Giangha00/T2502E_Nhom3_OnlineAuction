namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserDetailsPageViewModel
{
    public UserDetailViewModel Profile { get; set; } = new();

    public UserFormViewModel? EditForm { get; set; }

    public bool CanEdit { get; set; }
}
