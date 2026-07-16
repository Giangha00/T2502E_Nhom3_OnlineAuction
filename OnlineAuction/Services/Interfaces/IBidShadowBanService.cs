namespace OnlineAuction.Services.Interfaces;

public interface IBidShadowBanService
{
    Task<bool> IsShadowBannedAsync(int userId, CancellationToken cancellationToken = default);

    Task ApplyShadowBanAsync(
        int userId,
        TimeSpan duration,
        string reason,
        CancellationToken cancellationToken = default);
}
