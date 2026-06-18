namespace OnlineAuction.Helpers;

public static class DateTimeUtilities
{
    /// <summary>
    /// MySQL datetime columns are read as <see cref="DateTimeKind.Unspecified"/>.
    /// Treat those values as UTC instead of converting from local time.
    /// </summary>
    public static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    public static bool IsInFutureUtc(DateTime value) =>
        AsUtc(value) > DateTime.UtcNow;

    public static TimeSpan RemainingUtc(DateTime endDate) =>
        AsUtc(endDate) - DateTime.UtcNow;

    public static string ToUtcIsoString(DateTime value) =>
        AsUtc(value).ToString("o");
}
