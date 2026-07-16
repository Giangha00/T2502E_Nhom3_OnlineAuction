using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class GmailEmailSender : IEmailSender, IEmailVerificationService
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly EmailSettings _emailSettings;
    private readonly PasswordResetOtpSettings _otpSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GmailEmailSender> _logger;

    public GmailEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        IOptions<EmailSettings> emailOptions,
        IOptions<PasswordResetOtpSettings> otpOptions,
        IWebHostEnvironment environment,
        ILogger<GmailEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _emailSettings = emailOptions.Value;
        _otpSettings = otpOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task<bool> SendPasswordResetOtpAsync(
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (_otpSettings.UseMockOtpSender && _environment.IsDevelopment())
        {
            _logger.LogWarning("Mock password reset OTP for {Email}: {Otp}", to, otpCode);
            return Task.FromResult(true);
        }

        var subject = IsVietnamese(locale)
            ? "Mã đặt lại mật khẩu RareCard"
            : "Your RareCard password reset code";
        var html = BuildPasswordResetHtml(fullName, otpCode, expiryMinutes, locale);

        return SendEmailAsync(to, subject, html, cancellationToken);
    }

    public Task<bool> SendConfirmationAsync(
        string to,
        string fullName,
        string confirmUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var subject = IsVietnamese(locale)
            ? "Kích hoạt tài khoản OnlineAuction"
            : "Activate your OnlineAuction account";
        var html = BuildConfirmationHtml(fullName, confirmUrl, locale);

        return SendEmailAsync(to, subject, html, cancellationToken);
    }

    private async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string html,
        CancellationToken cancellationToken)
    {
        if (ShouldUseMockSender())
        {
            _logger.LogWarning(
                "Mock email to {Email}. Subject: {Subject}. Content: {Content}",
                to,
                subject,
                html);
            return true;
        }

        var senderEmail = ResolveSenderEmail();
        var senderName = ResolveSenderName();

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            return HandleMissingSender(to, subject, html);
        }

        if (await TrySendViaGmailApiAsync(to, subject, html, senderEmail, senderName, cancellationToken))
        {
            return true;
        }

        if (await TrySendViaSmtpAsync(to, subject, html, senderEmail, senderName, cancellationToken))
        {
            return true;
        }

        if (_environment.IsDevelopment())
        {
            _logger.LogWarning(
                "Email delivery unavailable. Development email to {Email}. Subject: {Subject}. Content: {Content}",
                to,
                subject,
                html);
            return true;
        }

        _logger.LogError(
            "Email delivery failed for {Email}. Check Gmail OAuth refresh token or SMTP settings.",
            to);
        return false;
    }

    private bool ShouldUseMockSender() =>
        _emailSettings.UseMockEmailSender && _environment.IsDevelopment();

    private bool HandleMissingSender(string to, string subject, string html)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogWarning(
                "Email sender config is missing. Development email to {Email}. Subject: {Subject}. Content: {Content}",
                to,
                subject,
                html);
            return true;
        }

        _logger.LogError("Email sender config is missing.");
        return false;
    }

    private async Task<bool> TrySendViaGmailApiAsync(
        string to,
        string subject,
        string html,
        string senderEmail,
        string senderName,
        CancellationToken cancellationToken)
    {
        var clientId = ResolveOAuthSetting(_emailSettings.Gmail.ClientId, "Email:Gmail:ClientId", "EmailVerification:Gmail:ClientId", "GMAIL_CLIENT_ID");
        var clientSecret = ResolveOAuthSetting(_emailSettings.Gmail.ClientSecret, "Email:Gmail:ClientSecret", "EmailVerification:Gmail:ClientSecret", "GMAIL_CLIENT_SECRET");
        var refreshToken = ResolveOAuthSetting(_emailSettings.Gmail.RefreshToken, "Email:Gmail:RefreshToken", "EmailVerification:Gmail:RefreshToken", "GMAIL_REFRESH_TOKEN");

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
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

            var rawMessage = CreateRawEmail(senderEmail, senderName, to, subject, html);

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
                "Send Gmail API email failed. StatusCode: {StatusCode}. Body: {Body}",
                response.StatusCode,
                Truncate(responseBody, 512));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gmail API email send failed.");
        }

        return false;
    }

    private async Task<bool> TrySendViaSmtpAsync(
        string to,
        string subject,
        string html,
        string senderEmail,
        string senderName,
        CancellationToken cancellationToken)
    {
        var smtp = _emailSettings.Smtp;
        if (!smtp.Enabled)
        {
            return false;
        }

        var username = FirstNonEmpty(smtp.Username, senderEmail);
        var password = FirstNonEmpty(
            smtp.Password,
            _configuration["Email:Smtp:Password"],
            Environment.GetEnvironmentVariable("SMTP_PASSWORD"));

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP is enabled but Username/Password is missing.");
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = html,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent via SMTP to {Email}.", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP email send failed for {Email}.", to);
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

    private string? ResolveSenderEmail()
    {
        return FirstNonEmpty(
            _emailSettings.Smtp.SenderEmail,
            _emailSettings.Gmail.SenderEmail,
            ResolveOAuthSetting(null, "Email:Gmail:SenderEmail", "EmailVerification:Gmail:SenderEmail", "SENDER_EMAIL"));
    }

    private string ResolveSenderName()
    {
        return FirstNonEmpty(
            _emailSettings.Smtp.SenderName,
            _emailSettings.Gmail.SenderName,
            ResolveOAuthSetting(null, "Email:Gmail:SenderName", "EmailVerification:Gmail:SenderName", "SENDER_NAME"),
            "RareCard Auction House")!;
    }

    private string? ResolveOAuthSetting(
        string? boundValue,
        string primaryConfigKey,
        string legacyConfigKey,
        string environmentKey)
    {
        if (!string.IsNullOrWhiteSpace(boundValue))
        {
            return boundValue.Trim();
        }

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

    private static string CreateRawEmail(
        string senderEmail,
        string senderName,
        string to,
        string subject,
        string html)
    {
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

    private static string BuildConfirmationHtml(string fullName, string confirmUrl, string locale)
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
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
