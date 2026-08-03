using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class BidChallengeService : IBidChallengeService
{
    private readonly IDistributedCache _cache;
    private readonly BidFraudDetectionSettings _settings;
    private readonly ILogger<BidChallengeService> _logger;

    public BidChallengeService(
        IDistributedCache cache,
        IOptions<BidFraudDetectionSettings> settings,
        ILogger<BidChallengeService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<BidChallengeRequirement> GetRequirementAsync(
        int userId,
        int bidsInCurrentWindow,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.ChallengeEnabled
            || string.Equals(_settings.ChallengeProvider, BidChallengeProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return new BidChallengeRequirement(false, BidChallengeProviders.None);
        }

        var overSoftLimit = bidsInCurrentWindow >= _settings.ChallengeAfterBidsPerMinute;
        if (!_settings.ChallengeAfterFraudAlert)
        {
            return new BidChallengeRequirement(overSoftLimit, _settings.ChallengeProvider);
        }

        var flagged = await _cache.GetStringAsync(Key(userId), cancellationToken);
        var required = !string.IsNullOrEmpty(flagged) || overSoftLimit;

        return new BidChallengeRequirement(required, _settings.ChallengeProvider);
    }

    public async Task RequireChallengeAsync(
        int userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.ChallengeEnabled || !_settings.ChallengeAfterFraudAlert)
        {
            return;
        }

        if (string.Equals(_settings.ChallengeProvider, BidChallengeProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var minutes = Math.Max(1, _settings.ChallengeRequiredMinutes);
        await _cache.SetStringAsync(
            Key(userId),
            reason,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
            },
            cancellationToken);

        _logger.LogInformation(
            "Challenge required for user {UserId}. Reason: {Reason}",
            userId,
            reason);
    }

    public Task<BidChallengeVerificationResult> VerifyAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.ChallengeEnabled
            || string.Equals(_settings.ChallengeProvider, BidChallengeProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new BidChallengeVerificationResult(true));
        }

        if (string.Equals(_settings.ChallengeProvider, BidChallengeProviders.Stub, StringComparison.OrdinalIgnoreCase))
        {
            var accepted = _settings.StubChallengeAcceptedTokens ?? [];
            var valid = !string.IsNullOrWhiteSpace(token)
                && accepted.Any(item => string.Equals(item, token.Trim(), StringComparison.Ordinal));

            return Task.FromResult(valid
                ? new BidChallengeVerificationResult(true)
                : new BidChallengeVerificationResult(false, "Challenge verification failed."));
        }

        _logger.LogWarning("Unknown challenge provider {Provider}.", _settings.ChallengeProvider);
        return Task.FromResult(new BidChallengeVerificationResult(false, "Challenge provider is not configured."));
    }

    public Task ClearRequirementAsync(int userId, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(Key(userId), cancellationToken);

    private static string Key(int userId) => $"bid-challenge:{userId}";
}
