namespace OnlineAuction.Helpers;

public static class DashboardFilterValidator
{
    public const int DefaultFilterDays = 7;

    public const int MaxFilterDays = 365;

    public const string ErrorDateFromAfterDateTo = "Admin_Dashboard_Filter_Error_DateFromAfterDateTo";

    public const string ErrorRangeTooLong = "Admin_Dashboard_Filter_Error_RangeTooLong";

    public static DashboardFilterValidationResult Validate(DateTime dateFrom, DateTime dateTo)
    {
        var from = dateFrom.Date;
        var to = dateTo.Date;

        if (from > to)
        {
            return DashboardFilterValidationResult.Invalid(ErrorDateFromAfterDateTo);
        }

        var periodDays = (to - from).Days + 1;
        if (periodDays > MaxFilterDays)
        {
            return DashboardFilterValidationResult.Invalid(ErrorRangeTooLong);
        }

        return DashboardFilterValidationResult.Valid();
    }
}

public readonly struct DashboardFilterValidationResult
{
    public bool IsValid { get; init; }

    public string? ErrorKey { get; init; }

    public static DashboardFilterValidationResult Valid() =>
        new() { IsValid = true };

    public static DashboardFilterValidationResult Invalid(string errorKey) =>
        new() { IsValid = false, ErrorKey = errorKey };
}
