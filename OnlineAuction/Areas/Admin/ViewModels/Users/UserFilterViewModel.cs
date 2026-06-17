using OnlineAuction.Enums;

namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserFilterViewModel
{
    public string? Search { get; set; }

    public string? DateRange { get; set; }

    public string? SortOrder { get; set; }

    public UserRole? Role { get; set; }

    public UserStatus? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
