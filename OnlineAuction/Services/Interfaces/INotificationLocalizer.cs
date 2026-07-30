namespace OnlineAuction.Services.Interfaces;

/// <summary>
/// Builds storable notification keys and resolves them for the current UI culture.
/// </summary>
public interface INotificationLocalizer
{
    /// <summary>Returns the resource key for storage (not the translated text).</summary>
    string this[string name] { get; }

    /// <summary>Returns an encoded key+args payload for storage.</summary>
    string Format(string name, params object[] args);

    /// <summary>Resolves a stored key/payload into the current UI culture.</summary>
    string Resolve(string? stored, string? argsJson = null);
}
