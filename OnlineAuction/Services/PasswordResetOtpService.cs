using Microsoft.Extensions.Caching.Memory;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class PasswordResetOtpService : IPasswordResetOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan VerifiedLifetime = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public PasswordResetOtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateOtp(string email, string resetToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var otp = Random.Shared.Next(100_000, 1_000_000).ToString("D6");

        _cache.Set(
            OtpCacheKey(normalizedEmail),
            new OtpEntry(otp, resetToken),
            OtpLifetime);

        _cache.Remove(VerifiedCacheKey(normalizedEmail));

        return otp;
    }

    public bool VerifyOtp(string email, string otp)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (!_cache.TryGetValue(OtpCacheKey(normalizedEmail), out OtpEntry? entry) || entry is null)
        {
            return false;
        }

        if (!string.Equals(entry.Otp, otp.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        _cache.Remove(OtpCacheKey(normalizedEmail));
        _cache.Set(VerifiedCacheKey(normalizedEmail), entry.ResetToken, VerifiedLifetime);

        return true;
    }

    public bool TryConsumeVerifiedToken(string email, out string? resetToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (_cache.TryGetValue(VerifiedCacheKey(normalizedEmail), out string? token) && !string.IsNullOrWhiteSpace(token))
        {
            resetToken = token;
            _cache.Remove(VerifiedCacheKey(normalizedEmail));
            return true;
        }

        resetToken = null;
        return false;
    }

    public void Clear(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        _cache.Remove(OtpCacheKey(normalizedEmail));
        _cache.Remove(VerifiedCacheKey(normalizedEmail));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string OtpCacheKey(string email) => $"pwd-reset-otp:{email}";

    private static string VerifiedCacheKey(string email) => $"pwd-reset-verified:{email}";

    private sealed record OtpEntry(string Otp, string ResetToken);
}
