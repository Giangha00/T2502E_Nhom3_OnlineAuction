namespace OnlineAuction.Configurations;

public static class PermissionCodes
{
    public const string DashboardView = "dashboard.view";

    public const string AuctionsView = "auctions.view";
    public const string AuctionsManage = "auctions.manage";
    public const string AuctionsVerify = "auctions.verify";

    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";

    public const string CategoriesManage = "categories.manage";

    public const string ProductsManage = "products.manage";

    public const string ComplaintsReview = "complaints.review";

    public const string PermissionsView = "permissions.view";
    public const string PermissionsManage = "permissions.manage";

    public const string PolicyPrefix = "Permission:";

    public static readonly string[] All =
    [
        DashboardView,
        AuctionsView,
        AuctionsManage,
        AuctionsVerify,
        UsersView,
        UsersManage,
        CategoriesManage,
        ProductsManage,
        ComplaintsReview,
        PermissionsView,
        PermissionsManage
    ];

    public static string ToPolicyName(string permissionCode) => $"{PolicyPrefix}{permissionCode}";
}
