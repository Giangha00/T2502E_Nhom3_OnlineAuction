namespace OnlineAuction.Entities;

public class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
