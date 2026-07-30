using System.Globalization;
using Microsoft.Extensions.Localization;
using OnlineAuction.Helpers;
using Xunit;

namespace OnlineAuction.Tests;

public class NotificationLocalizationTests
{
    [Fact]
    public void Encode_And_ToStorage_PreservesKeyAndArgs()
    {
        var encoded = NotificationLocalization.Encode("Notification_Outbid_Message", "Charizard", 12.5m);
        var (storage, argsJson) = NotificationLocalization.ToStorage(encoded);

        Assert.Equal("Notification_Outbid_Message", storage);
        Assert.Contains("Charizard", argsJson);
        Assert.Contains("12.5", argsJson);
    }

    [Fact]
    public void Resolve_UsesCurrentUiCultureTemplate()
    {
        var localizer = new FakeLocalizer(new Dictionary<string, string>
        {
            ["Notification_Outbid_Message"] = "You were outbid on {0}."
        });

        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var text = NotificationLocalization.Resolve(
                localizer,
                "Notification_Outbid_Message",
                """["Nami"]""");

            Assert.Equal("You were outbid on Nami.", text);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Resolve_LeavesLegacyPlainTextUnchanged()
    {
        var localizer = new FakeLocalizer(new Dictionary<string, string>());
        var text = NotificationLocalization.Resolve(localizer, "Custom admin note");
        Assert.Equal("Custom admin note", text);
    }

    private sealed class FakeLocalizer : IStringLocalizer
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public FakeLocalizer(IReadOnlyDictionary<string, string> values) => _values = values;

        public LocalizedString this[string name] =>
            _values.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _values.Select(pair => new LocalizedString(pair.Key, pair.Value));

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
