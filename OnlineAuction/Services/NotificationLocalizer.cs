using System.Globalization;
using Microsoft.Extensions.Localization;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class NotificationLocalizer : INotificationLocalizer
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public NotificationLocalizer(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string this[string name] => _localizer[name].Value;

    public string Format(string name, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, _localizer[name].Value, args);
}
