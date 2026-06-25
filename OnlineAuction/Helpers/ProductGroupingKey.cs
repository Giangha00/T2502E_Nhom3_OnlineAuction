using System.Text.RegularExpressions;
using OnlineAuction.Entities;

namespace OnlineAuction.Helpers;

public static partial class ProductGroupingKey
{
    public static string GetKey(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.SetName) && !string.IsNullOrWhiteSpace(product.CardNumber))
        {
            return CategorySlug.NormalizeForCompare(
                $"{product.CategoryId}|{product.SetName.Trim()}|{product.CardNumber.Trim()}");
        }

        return CategorySlug.NormalizeForCompare(product.Name);
    }

    public static string BuildTemplateName(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.SetName) && !string.IsNullOrWhiteSpace(product.CardNumber))
        {
            var cardName = ExtractCardName(product.Name);
            if (!string.IsNullOrWhiteSpace(cardName))
            {
                return $"{cardName} ({product.SetName})";
            }

            return $"{product.SetName} #{product.CardNumber}";
        }

        return product.Name.Trim();
    }

    public static string ExtractCardName(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return string.Empty;
        }

        var normalized = GradePrefixRegex().Replace(productName.Trim(), string.Empty).Trim();
        normalized = YearSuffixRegex().Replace(normalized, string.Empty).Trim();

        return string.IsNullOrWhiteSpace(normalized) ? productName.Trim() : normalized;
    }

    [GeneratedRegex(@"^(?:PSA|CGC|BGS|SGC|AGS|HGA|GMA|CSG)\s*[\d.]+\s+|^Near\s+Mint\s+", RegexOptions.IgnoreCase)]
    private static partial Regex GradePrefixRegex();

    [GeneratedRegex(@"\s+\d{4}$")]
    private static partial Regex YearSuffixRegex();
}
