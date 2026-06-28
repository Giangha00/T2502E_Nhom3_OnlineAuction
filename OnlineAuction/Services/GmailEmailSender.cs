using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class GmailEmailSender : IEmailSender
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly PasswordResetOtpSettings _otpSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GmailEmailSender> _logger;

    public GmailEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        IOptions<PasswordResetOtpSettings> otpOptions,
        IWebHostEnvironment environment,
        ILogger<GmailEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _otpSettings = otpOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetOtpAsync(
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (_otpSettings.UseMockOtpSender)
        {
            // Mock sender is for local learning/testing only. Do not enable it in production.
            _logger.LogWarning("Mock password reset OTP for {Email}: {Otp}", to, otpCode);
            return true;
        }

        var clientId = GetSetting("Email:Gmail:ClientId", "EmailVerification:Gmail:ClientId", "GMAIL_CLIENT_ID");
        var clientSecret = GetSetting("Email:Gmail:ClientSecret", "EmailVerification:Gmail:ClientSecret", "GMAIL_CLIENT_SECRET");
        var refreshToken = GetSetting("Email:Gmail:RefreshToken", "EmailVerification:Gmail:RefreshToken", "GMAIL_REFRESH_TOKEN");
        var senderEmail = GetSetting("Email:Gmail:SenderEmail", "EmailVerification:Gmail:SenderEmail", "SENDER_EMAIL");
        var senderName = GetSetting("Email:Gmail:SenderName", "EmailVerification:Gmail:SenderName", "SENDER_NAME")
            ?? "RareCard Auction House";

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(senderEmail))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning(
                    "Gmail config is missing. Development OTP for {Email}: {Otp}",
                    to,
                    otpCode);
                return true;
            }

            _logger.LogError("Gmail email config is missing.");
            return false;
        }

        try
        {
            var accessToken = await RequestAccessTokenAsync(
                clientId,
                clientSecret,
                refreshToken,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return false;
            }

            var rawMessage = CreateRawEmail(
                senderEmail,
                senderName,
                to,
                fullName,
                otpCode,
                expiryMinutes,
                locale);

            using var request = new HttpRequestMessage(HttpMethod.Post, GmailSendEndpoint)
            {
                Content = JsonContent.Create(new GmailSendRequest(rawMessage))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Send Gmail password reset OTP failed. StatusCode: {StatusCode}. Body: {Body}",
                response.StatusCode,
                Truncate(responseBody, 512));

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail password reset OTP error.");
            return false;
        }
    }

    private async Task<string?> RequestAccessTokenAsync(
        string clientId,
        string clientSecret,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        using var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var token = DeserializeTokenResponse(responseBody);

        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            return token.AccessToken;
        }

        _logger.LogWarning(
            "Request Gmail access token failed. StatusCode: {StatusCode}. Body: {Body}",
            response.StatusCode,
            Truncate(responseBody, 512));

        return null;
    }

    private static string CreateRawEmail(
        string senderEmail,
        string senderName,
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale)
    {
        var subject = IsVietnamese(locale)
            ? "Mã đặt lại mật khẩu RareCard"
            : "Your RareCard password reset code";
        var html = BuildPasswordResetHtml(fullName, otpCode, expiryMinutes, locale);

        var message = string.Join("\r\n",
        [
            $"From: {EncodeHeader(senderName)} <{senderEmail}>",
            $"To: {to}",
            $"Subject: {EncodeHeader(subject)}",
            "MIME-Version: 1.0",
            "Content-Type: text/html; charset=UTF-8",
            "",
            html
        ]);

        return Base64UrlEncode(Encoding.UTF8.GetBytes(message));
    }

    private static string BuildPasswordResetHtml(
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale)
    {
        var isVietnamese = IsVietnamese(locale);
        var fallbackName = isVietnamese ? "bạn" : "there";
        var safeName = HtmlEncoder.Default.Encode(
            string.IsNullOrWhiteSpace(fullName) ? fallbackName : fullName.Trim());
        var safeOtp = HtmlEncoder.Default.Encode(otpCode);

        if (isVietnamese)
        {
            return $"""
                <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #0f172a;">
                  <h2>RareCard Auction House</h2>
                  <p>Xin chào {safeName},</p>
                  <p>Mã đặt lại mật khẩu của bạn là:</p>
                  <p style="font-size: 28px; font-weight: 800; letter-spacing: 8px;">{safeOtp}</p>
                  <p>Mã này có hiệu lực trong {expiryMinutes} phút.</p>
                  <p>Nếu bạn không yêu cầu đổi mật khẩu, hãy bỏ qua email này và không chia sẻ mã cho bất kỳ ai.</p>
                </div>
                """;
        }

        return $"""
            <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #0f172a;">
              <h2>RareCard Auction House</h2>
              <p>Hello {safeName},</p>
              <p>Your password reset code is:</p>
              <p style="font-size: 28px; font-weight: 800; letter-spacing: 8px;">{safeOtp}</p>
              <p>This code expires in {expiryMinutes} minutes.</p>
              <p>If you did not request a password reset, ignore this email and do not share the code.</p>
            </div>
            """;
    }

    private string? GetSetting(string primaryConfigKey, string legacyConfigKey, string environmentKey)
    {
        var value = _configuration[primaryConfigKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = _configuration[legacyConfigKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = Environment.GetEnvironmentVariable(environmentKey);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string EncodeHeader(string value)
    {
        return value.All(ch => ch <= 127)
            ? value
            : $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static GmailTokenResponse? DeserializeTokenResponse(string responseBody)
    {
        try
        {
            return JsonSerializer.Deserialize<GmailTokenResponse>(responseBody);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsVietnamese(string locale) =>
        locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record GmailSendRequest([property: JsonPropertyName("raw")] string Raw);

    private sealed record GmailTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);
}
