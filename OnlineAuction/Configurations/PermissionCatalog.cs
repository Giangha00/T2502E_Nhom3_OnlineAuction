using OnlineAuction.Areas.Admin.ViewModels.Permissions;

namespace OnlineAuction.Configurations;

/// <summary>
/// In-code permission catalog. Assigned permissions are stored as Identity user claims
/// (<see cref="PermissionClaimTypes.Permission"/>), not in custom tables.
/// </summary>
public static class PermissionCatalog
{
    private sealed record Entry(string Code, string Name, string Module, string? Description);

    private static readonly Entry[] Entries =
    [
        new(PermissionCodes.DashboardView, "View Dashboard", "Dashboard", "Access admin dashboard and exports"),
        new(PermissionCodes.AuctionsView, "View Auctions", "Auctions", "View auction list and details"),
        new(PermissionCodes.AuctionsManage, "Manage Auctions", "Auctions", "Create, edit, and delete auctions"),
        new(PermissionCodes.AuctionsVerify, "Verify Auctions", "Auctions", "Approve or reject seller listings"),
        new(PermissionCodes.UsersView, "View Users", "Users", "View user list and profiles"),
        new(PermissionCodes.UsersManage, "Manage Users", "Users", "Create, edit, delete users and manage roles"),
        new(PermissionCodes.CategoriesManage, "Manage Categories", "Categories", "Full category CRUD"),
        new(PermissionCodes.ProductsManage, "Manage Products", "Products", "Product admin module"),
        new(PermissionCodes.ComplaintsReview, "Review Complaints", "Complaints", "Complaint review module"),
        new(PermissionCodes.PermissionsView, "View Permissions", "Permissions", "View permission catalog and assignments"),
        new(PermissionCodes.PermissionsManage, "Manage Permissions", "Permissions", "Update user permission assignments")
    ];

    public static IReadOnlyList<PermissionItemViewModel> All { get; } = Entries
        .Select(entry => new PermissionItemViewModel
        {
            Code = entry.Code,
            Name = entry.Name,
            Module = entry.Module,
            Description = entry.Description
        })
        .ToList();

    public static bool IsKnown(string permissionCode) =>
        PermissionCodes.All.Contains(permissionCode);
}
