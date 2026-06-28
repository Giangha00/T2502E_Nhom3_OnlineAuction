using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Services.Interfaces;
using OnlineAuction.Services.Results;

namespace OnlineAuction.Services;

public sealed class PasswordResetOtpService : IPasswordResetOtpService
{
    private const string PasswordResetPurpose = "password_reset";

    private readonly AuctionHouseDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IMemoryCache _cache;
    private readonly PasswordResetOtpSettings _settings;
    private readonly IdentityOptions _identityOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PasswordResetOtpService> _logger;

    public PasswordResetOtpService(
        AuctionHouseDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IMemoryCache cache,
        IOptions<PasswordResetOtpSettings> options,
        IOptions<IdentityOptions> identityOptions,
        IWebHostEnvironment environment,
        ILogger<PasswordResetOtpService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailSender = emailSender;
        _cache = cache;
        _settings = options.Value;
        _identityOptions = identityOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<PasswordResetOtpSendResult> GenerateAndSendAsync(
        string email,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var maskedEmail = MaskEmail(normalizedEmail);

        if (IsInCooldown(normalizedEmail, out var retryAfterSeconds))
        {
            return new PasswordResetOtpSendResult(
                PasswordResetOtpSendStatus.Cooldown,
                maskedEmail,
                RetryAfterSeconds: retryAfterSeconds);
        }

        if (IsHourlyRateLimited(normalizedEmail))
        {
            return new PasswordResetOtpSendResult(
                PasswordResetOtpSendStatus.RateLimited,
                maskedEmail);
        }

        RememberSendAttempt(normalizedEmail);

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (!await CanReceivePasswordResetOtpAsync(user))
        {
            // Do not reveal whether an email exists, belongs to admin, or is inactive.
            return new PasswordResetOtpSendResult(PasswordResetOtpSendStatus.Sent, maskedEmail);
        }

        await InvalidateActiveOtpsAsync(user!.Id, cancellationToken);

        var otpCode = CreateNumericCode(_settings.CodeLength);
        var salt = CreateSalt();
        var otp = new UserOtpCode
        {
            UserId = user.Id,
            CodeHash = HashOtp(otpCode, salt),
            Salt = salt,
            Purpose = PasswordResetPurpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            AttemptCount = 0,
            MaxAttempts = _settings.MaxAttempts,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserOtpCodes.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sent = await _emailSender.SendPasswordResetOtpAsync(
            user.Email ?? normalizedEmail,
            user.FullName,
            otpCode,
            _settings.ExpiryMinutes,
            locale,
            cancellationToken);

        if (!sent)
        {
            // If the email was not sent, invalidate the DB row so a code the user never received cannot be used later.
            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PasswordResetOtpSendResult(PasswordResetOtpSendStatus.Failed, maskedEmail);
        }

        var developmentOtp = _settings.UseMockOtpSender && _environment.IsDevelopment()
            ? otpCode
            : null;

        return new PasswordResetOtpSendResult(
            PasswordResetOtpSendStatus.Sent,
            maskedEmail,
            developmentOtp);
    }

    public async Task<PasswordResetOtpVerifyResult> VerifyAsync(
        string email,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var maskedEmail = MaskEmail(normalizedEmail);
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (!await CanReceivePasswordResetOtpAsync(user))
        {
            return new PasswordResetOtpVerifyResult(
                PasswordResetOtpVerifyStatus.Invalid,
                MaskedEmail: maskedEmail);
        }

        var otp = await _dbContext.UserOtpCodes
            .Where(code =>
                code.UserId == user!.Id &&
                code.Purpose == PasswordResetPurpose &&
                !code.IsUsed)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            return new PasswordResetOtpVerifyResult(
                PasswordResetOtpVerifyStatus.Invalid,
                MaskedEmail: maskedEmail);
        }

        if (otp.ExpiresAt <= DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PasswordResetOtpVerifyResult(
                PasswordResetOtpVerifyStatus.Expired,
                MaskedEmail: maskedEmail);
        }

        if (otp.AttemptCount >= otp.MaxAttempts)
        {
            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PasswordResetOtpVerifyResult(
                PasswordResetOtpVerifyStatus.MaxAttemptsReached,
                MaskedEmail: maskedEmail);
        }

        if (!MatchesOtp(otp, otpCode))
        {
            otp.AttemptCount++;

            if (otp.AttemptCount >= otp.MaxAttempts)
            {
                otp.IsUsed = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new PasswordResetOtpVerifyResult(
                    PasswordResetOtpVerifyStatus.MaxAttemptsReached,
                    MaskedEmail: maskedEmail);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PasswordResetOtpVerifyResult(
                PasswordResetOtpVerifyStatus.Invalid,
                MaskedEmail: maskedEmail);
        }

        // A correct OTP only proves this step. The password reset token is created later in AuthController
        // when the user submits the new password, so no sensitive reset token sits in session.
        return new PasswordResetOtpVerifyResult(
            PasswordResetOtpVerifyStatus.Valid,
            user!.Id,
            otp.Id,
            maskedEmail);
    }

    public async Task<bool> IsVerifiedOtpStillUsableAsync(
        int userId,
        int otpId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserOtpCodes.AnyAsync(
            otp =>
                otp.Id == otpId &&
                otp.UserId == userId &&
                otp.Purpose == PasswordResetPurpose &&
                !otp.IsUsed &&
                otp.ExpiresAt > DateTime.UtcNow &&
                otp.AttemptCount < otp.MaxAttempts,
            cancellationToken);
    }

    public async Task InvalidateAsync(
        int userId,
        int otpId,
        CancellationToken cancellationToken = default)
    {
        var otp = await _dbContext.UserOtpCodes
            .FirstOrDefaultAsync(
                code => code.Id == otpId && code.UserId == userId,
                cancellationToken);

        if (otp is null)
        {
            return;
        }

        otp.IsUsed = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> CanReceivePasswordResetOtpAsync(ApplicationUser? user)
    {
        if (user is null || user.Status != UserStatus.Active)
        {
            return false;
        }

        if (user.Role == UserRole.Admin || await _userManager.IsInRoleAsync(user, UserRole.Admin.ToString()))
        {
            return false;
        }

        return !_identityOptions.SignIn.RequireConfirmedEmail || user.EmailConfirmed;
    }

    private async Task InvalidateActiveOtpsAsync(int userId, CancellationToken cancellationToken)
    {
        var activeOtps = await _dbContext.UserOtpCodes
            .Where(otp =>
                otp.UserId == userId &&
                otp.Purpose == PasswordResetPurpose &&
                !otp.IsUsed &&
                otp.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var otp in activeOtps)
        {
            otp.IsUsed = true;
        }
    }

    private bool IsInCooldown(string normalizedEmail, out int retryAfterSeconds)
    {
        if (_cache.TryGetValue(CooldownCacheKey(normalizedEmail), out DateTime untilUtc))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((untilUtc - DateTime.UtcNow).TotalSeconds));
            return retryAfterSeconds > 0;
        }

        retryAfterSeconds = 0;
        return false;
    }

    private bool IsHourlyRateLimited(string normalizedEmail)
    {
        return _cache.TryGetValue(ResendCountCacheKey(normalizedEmail), out int count)
            && count >= _settings.MaxResendsPerHour;
    }

    private void RememberSendAttempt(string normalizedEmail)
    {
        var cooldownUntil = DateTime.UtcNow.AddSeconds(_settings.ResendCooldownSeconds);
        _cache.Set(
            CooldownCacheKey(normalizedEmail),
            cooldownUntil,
            TimeSpan.FromSeconds(_settings.ResendCooldownSeconds));

        var resendKey = ResendCountCacheKey(normalizedEmail);
        _cache.TryGetValue(resendKey, out int currentCount);
        _cache.Set(resendKey, currentCount + 1, TimeSpan.FromHours(1));
    }

    private static string CreateNumericCode(int codeLength)
    {
        var normalizedLength = Math.Clamp(codeLength, 4, 8);
        var maxExclusive = (int)Math.Pow(10, normalizedLength);
        var minInclusive = maxExclusive / 10;
        var value = RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
        return value.ToString($"D{normalizedLength}");
    }

    private static string CreateSalt()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashOtp(string otpCode, string salt)
    {
        var input = Encoding.UTF8.GetBytes($"{salt}:{otpCode.Trim()}");
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
    }

    private static bool MatchesOtp(UserOtpCode otp, string otpCode)
    {
        var expected = Convert.FromHexString(otp.CodeHash);
        var actual = Convert.FromHexString(HashOtp(otpCode, otp.Salt));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2 || parts[0].Length <= 2)
        {
            return email;
        }

        return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
    }

    private static string CooldownCacheKey(string email) => $"pwd-reset-otp-cooldown:{email}";

    private static string ResendCountCacheKey(string email) =>
        $"pwd-reset-otp-resend-count:{email}:{DateTime.UtcNow:yyyyMMddHH}";
}
