using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace OnlineAuction.Helpers;

/// <summary>
/// Stores notification copy as resource keys (+ format args) so title/message
/// can be resolved dynamically for the current UI culture at read time.
/// </summary>
public static class NotificationLocalization
{
    public const string Marker = "§LOC§";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static bool IsResourceKey(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 160
        && !value.Contains(' ', StringComparison.Ordinal)
        && (value.StartsWith("Notification_", StringComparison.Ordinal)
            || value.StartsWith("Common_", StringComparison.Ordinal)
            || value.StartsWith("Js_", StringComparison.Ordinal));

    public static string Encode(string key, params object[] args)
    {
        if (args is null || args.Length == 0)
        {
            return key;
        }

        var payload = JsonSerializer.Serialize(
            args.Select(FormatArg).ToArray(),
            JsonOptions);
        return $"{Marker}{key}{Marker}{payload}";
    }

    public static bool TryDecode(string? value, out string key, out string[] args)
    {
        key = string.Empty;
        args = [];

        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Marker, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = value.Split(Marker, StringSplitOptions.None);
        // "", key, json
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        key = parts[1].Trim();
        try
        {
            args = JsonSerializer.Deserialize<string[]>(parts[2], JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            args = [];
        }

        return IsResourceKey(key);
    }

    public static (string StorageText, string? ArgsJson) ToStorage(string? titleOrMessage)
    {
        if (string.IsNullOrWhiteSpace(titleOrMessage))
        {
            return (string.Empty, null);
        }

        var trimmed = titleOrMessage.Trim();
        if (TryDecode(trimmed, out var key, out var args))
        {
            return (key, args.Length == 0 ? null : JsonSerializer.Serialize(args, JsonOptions));
        }

        if (IsResourceKey(trimmed))
        {
            return (trimmed, null);
        }

        // Legacy / free-form copy (business errors, admin notes, etc.)
        return (trimmed, null);
    }

    public static string Resolve(
        IStringLocalizer localizer,
        string? stored,
        string? argsJson = null)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        string key;
        string[] args;

        if (TryDecode(stored, out key, out args))
        {
            // Encoded payload still present on legacy rows.
        }
        else if (IsResourceKey(stored))
        {
            key = stored.Trim();
            args = ParseArgsJson(argsJson);
        }
        else
        {
            return stored;
        }

        var localized = localizer[key];
        if (localized.ResourceNotFound
            || string.Equals(localized.Value, key, StringComparison.Ordinal))
        {
            return stored;
        }

        if (args.Length == 0)
        {
            return localized.Value;
        }

        try
        {
            return string.Format(CultureInfo.CurrentUICulture, localized.Value, args.Cast<object>().ToArray());
        }
        catch (FormatException)
        {
            return localized.Value;
        }
    }

    private static string[] ParseArgsJson(string? argsJson)
    {
        if (string.IsNullOrWhiteSpace(argsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(argsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string FormatArg(object? value) =>
        value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
}
