namespace OnlineAuction.Areas.Admin.ViewModels.Permissions;

public class PermissionManagementViewModel
{
    public IReadOnlyList<PermissionItemViewModel> Permissions { get; init; } = [];

    public IReadOnlyList<UserPermissionRowViewModel> AssignableUsers { get; init; } = [];

    public int? SelectedUserId { get; init; }

    public IReadOnlyList<string> SelectedUserPermissionCodes { get; init; } = [];

    public bool CanManage { get; init; }
}

public class PermissionItemViewModel
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Module { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public class UserPermissionRowViewModel
{
    public int UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public int AssignedPermissionCount { get; set; }
}
