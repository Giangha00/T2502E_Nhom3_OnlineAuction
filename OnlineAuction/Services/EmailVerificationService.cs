using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendConfirmationAsync(
        string to,
        string fullName,
        string confirmUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
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
            _logger.LogError("Gmail email verification config is missing.");
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
                confirmUrl,
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
                "Send Gmail verification email failed. StatusCode: {StatusCode}. Body: {Body}",
                response.StatusCode,
                Truncate(responseBody, 512));

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail email verification error.");
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
        string confirmUrl,
        string locale)
    {
        var subject = IsVietnamese(locale)
            ? "Kích hoạt tài khoản OnlineAuction"
            : "Activate your OnlineAuction account";
        var html = BuildEmailHtml(fullName, confirmUrl, locale);

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

    private static string BuildEmailHtml(string fullName, string confirmUrl, string locale)
    {
        var isVietnamese = IsVietnamese(locale);
        var fallbackName = isVietnamese ? "bạn" : "there";
        var safeName = HtmlEncoder.Default.Encode(
            string.IsNullOrWhiteSpace(fullName) ? fallbackName : fullName.Trim());
        var safeConfirmUrl = HtmlEncoder.Default.Encode(confirmUrl);

        if (isVietnamese)
        {
            return $"""
                <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #0f172a;">
                  <h2>RareCard Auction House</h2>
                  <p>Xin chào {safeName},</p>
                  <p>Tài khoản của bạn đang ở trạng thái chờ kích hoạt.</p>
                  <p>Vui lòng bấm nút bên dưới để hoàn tất đăng ký:</p>
                  <p>
                    <a href="{safeConfirmUrl}"
                       style="display:inline-block;padding:12px 18px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:6px;">
                      Kích hoạt tài khoản
                    </a>
                  </p>
                  <p>Nếu nút không hoạt động, hãy copy link sau:</p>
                  <p style="word-break: break-all;">{safeConfirmUrl}</p>
                  <p>Link có hiệu lực trong 24 giờ.</p>
                </div>
                """;
        }

        return $"""
            <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #0f172a;">
              <h2>RareCard Auction House</h2>
              <p>Hello {safeName},</p>
              <p>Your account is waiting for email activation.</p>
              <p>Please click the button below to complete registration:</p>
              <p>
                <a href="{safeConfirmUrl}"
                   style="display:inline-block;padding:12px 18px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:6px;">
                  Activate account
                </a>
              </p>
              <p>If the button does not work, copy this link:</p>
              <p style="word-break: break-all;">{safeConfirmUrl}</p>
              <p>This link is valid for 24 hours.</p>
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

    private static bool IsVietnamese(string locale) =>
        locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

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

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record GmailSendRequest([property: JsonPropertyName("raw")] string Raw);

    private sealed record GmailTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);
}
