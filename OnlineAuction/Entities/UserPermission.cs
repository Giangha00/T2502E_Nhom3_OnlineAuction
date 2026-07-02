namespace OnlineAuction.Entities;

public class UserPermission
{
    public int UserId { get; set; }

    public int PermissionId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
