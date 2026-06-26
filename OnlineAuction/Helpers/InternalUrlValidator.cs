namespace OnlineAuction.Helpers;

public static class InternalUrlValidator
{
    public static bool IsValidInternalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        if (!url.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        return !url.Contains("://", StringComparison.Ordinal);
    }

    public static string? NormalizeOrNull(string? url) =>
        IsValidInternalUrl(url) ? url : null;
}
