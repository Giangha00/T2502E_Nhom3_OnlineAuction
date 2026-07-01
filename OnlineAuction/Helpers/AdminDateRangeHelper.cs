using System.Globalization;

namespace OnlineAuction.Helpers;

public static class AdminDateRangeHelper
{
    private const string DateFormat = "MM/dd/yyyy";

    public static (DateTime? StartDate, DateTime? EndDateExclusive) Parse(string? dateRange)
    {
        if (string.IsNullOrWhiteSpace(dateRange))
        {
            return (null, null);
        }

        var dates = dateRange.Split(" - ", StringSplitOptions.TrimEntries);
        if (dates.Length != 2)
        {
            return (null, null);
        }

        var isStartValid = DateTime.TryParseExact(
            dates[0],
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var startDate);

        var isEndValid = DateTime.TryParseExact(
            dates[1],
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var endDate);

        if (!isStartValid || !isEndValid)
        {
            return (null, null);
        }

        return (startDate.Date, endDate.Date.AddDays(1));
    }

    public static string Format(DateTime startInclusive, DateTime endInclusive) =>
        $"{startInclusive.ToString(DateFormat, CultureInfo.InvariantCulture)} - {endInclusive.ToString(DateFormat, CultureInfo.InvariantCulture)}";
}
