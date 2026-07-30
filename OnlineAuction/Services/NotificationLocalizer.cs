using Microsoft.Extensions.Localization;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

/// <summary>
/// Returns resource keys (and encoded format payloads) for storage.
/// Display-time resolution uses <see cref="Resolve"/> with the current UI culture.
/// </summary>
public sealed class NotificationLocalizer : INotificationLocalizer
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public NotificationLocalizer(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string this[string name] => name;

    public string Format(string name, params object[] args) =>
        NotificationLocalization.Encode(name, args);

    public string Resolve(string? stored, string? argsJson = null) =>
        NotificationLocalization.Resolve(_localizer, stored, argsJson);
}
