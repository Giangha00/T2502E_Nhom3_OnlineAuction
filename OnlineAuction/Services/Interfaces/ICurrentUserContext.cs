namespace OnlineAuction.Services.Interfaces;

public interface ICurrentUserContext
{
    Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default);

    Task<int?> GetAdminIdAsync(CancellationToken cancellationToken = default);
}
