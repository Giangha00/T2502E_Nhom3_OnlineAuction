namespace OnlineAuction.Areas.Admin.ViewModels.RolePermissions;

public class RolePermissionIndexViewModel
{
    public IReadOnlyList<RolePermissionRoleItemViewModel> Roles { get; set; } = [];
}

public class RolePermissionRoleItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int PermissionCount { get; set; }

    public bool IsSuperRole { get; set; }
}

public class RolePermissionEditViewModel
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public bool IsSuperRole { get; set; }

    public IReadOnlyList<RolePermissionModuleViewModel> Modules { get; set; } = [];
}

public class RolePermissionModuleViewModel
{
    public string Module { get; set; } = string.Empty;

    public IReadOnlyList<RolePermissionItemViewModel> Permissions { get; set; } = [];
}

public class RolePermissionItemViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSelected { get; set; }
}

public class RolePermissionSaveViewModel
{
    public int RoleId { get; set; }

    public List<int> PermissionIds { get; set; } = [];
}
