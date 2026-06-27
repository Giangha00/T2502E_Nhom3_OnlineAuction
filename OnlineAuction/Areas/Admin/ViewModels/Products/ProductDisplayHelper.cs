namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public static class ProductDisplayHelper
{
    public static string FormatProductCode(int productId) => $"PRD-{productId:D8}";
}
