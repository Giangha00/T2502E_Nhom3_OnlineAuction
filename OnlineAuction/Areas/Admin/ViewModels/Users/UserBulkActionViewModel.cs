using OnlineAuction.Enums;

namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserBulkActionViewModel
{
    public List<int> SelectedUserIds { get; set; } = [];

    public string Action { get; set; } = string.Empty;

    public UserRole? Role { get; set; }

    public UserStatus? Status { get; set; }
}