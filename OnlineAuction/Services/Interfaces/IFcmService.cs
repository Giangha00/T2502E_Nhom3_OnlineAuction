namespace OnlineAuction.Services.Interfaces;

public interface IFcmService
{
    Task SendToUserAsync(
        int userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string> dataPayload,
        CancellationToken cancellationToken = default);
}
