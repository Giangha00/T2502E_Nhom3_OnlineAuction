namespace OnlineAuction.Services.Interfaces;

/// <summary>
/// Resolves notification copy using the current UI culture (cookie/query).
/// </summary>
public interface INotificationLocalizer
{
    string this[string name] { get; }

    string Format(string name, params object[] args);
}
