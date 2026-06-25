using System.Globalization;

namespace OnlineAuction.Helpers;

public static class RelativeTimeHelper
{
    public static string Format(DateTime createdAtUtc, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var elapsed = DateTime.UtcNow - createdAtUtc;

        if (elapsed.TotalSeconds < 60)
        {
            return GetPhrase(culture, "Just now", "Vừa xong", "たった今", "방금");
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = (int)elapsed.TotalMinutes;
            return FormatUnit(culture, minutes, "mins ago", "phút trước", "分前", "분 전", "min ago", "phút trước", "分前", "분 전");
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = (int)elapsed.TotalHours;
            return FormatUnit(culture, hours, "hour ago", "giờ trước", "時間前", "시간 전", "hours ago", "giờ trước", "時間前", "시간 전");
        }

        if (elapsed.TotalDays < 2)
        {
            return GetPhrase(culture, "Yesterday", "Hôm qua", "昨日", "어제");
        }

        if (elapsed.TotalDays < 7)
        {
            var days = (int)elapsed.TotalDays;
            return FormatUnit(culture, days, "days ago", "ngày trước", "日前", "일 전", "day ago", "ngày trước", "日前", "일 전");
        }

        if (elapsed.TotalDays < 30)
        {
            var weeks = (int)(elapsed.TotalDays / 7);
            return FormatUnit(culture, weeks, "weeks ago", "tuần trước", "週間前", "주 전", "week ago", "tuần trước", "週間前", "주 전");
        }

        return createdAtUtc.ToLocalTime().ToString("d", culture);
    }

    private static string FormatUnit(
        CultureInfo culture,
        int count,
        string enPlural,
        string viPlural,
        string jaPlural,
        string koPlural,
        string enSingular,
        string viSingular,
        string jaSingular,
        string koSingular)
    {
        var isSingular = count == 1;
        return culture.Name switch
        {
            "vi-VN" => isSingular ? $"{count} {viSingular}" : $"{count} {viPlural}",
            "ja-JP" => $"{count}{jaPlural}",
            "ko-KR" => $"{count}{koPlural}",
            _ => isSingular ? $"{count} {enSingular}" : $"{count} {enPlural}"
        };
    }

    private static string GetPhrase(CultureInfo culture, string en, string vi, string ja, string ko) =>
        culture.Name switch
        {
            "vi-VN" => vi,
            "ja-JP" => ja,
            "ko-KR" => ko,
            _ => en
        };
}
