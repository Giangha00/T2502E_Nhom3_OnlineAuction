using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PayPalService : IPayPalService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly PayPalSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PayPalService> _logger;
    private readonly IHostEnvironment _environment;

    public PayPalService(
        HttpClient httpClient,
        IOptions<PayPalSettings> settings,
        IMemoryCache cache,
        ILogger<PayPalService> logger,
        IHostEnvironment environment)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
        _environment = environment;
    }

    public async Task<PayPalCreateOrderResult> CreateCheckoutOrderAsync(
        decimal totalAmount,
        string referenceId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return PayPalCreateOrderResult.Fail(GetNotConfiguredMessage());
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return PayPalCreateOrderResult.Fail("Unable to connect to PayPal. Please try again.");
        }

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId,
                    amount = new
                    {
                        currency_code = _settings.CurrencyCode,
                        value = FormatAmount(totalAmount)
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl,
                brand_name = "RareCard Auction",
                user_action = "PAY_NOW",
                shipping_preference = "NO_SHIPPING"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.ApiBaseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal create order request failed.");
            return PayPalCreateOrderResult.Fail("Unable to connect to PayPal. Please try again.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal create order failed with status {StatusCode}.", response.StatusCode);
            return PayPalCreateOrderResult.Fail("PayPal could not start checkout. Please try again.");
        }

        var order = JsonSerializer.Deserialize<PayPalOrderResponse>(body, JsonOptions);
        var approvalUrl = order?.Links?
            .FirstOrDefault(link => link.Rel.Equals("approve", StringComparison.OrdinalIgnoreCase))
            ?.Href;

        if (string.IsNullOrWhiteSpace(order?.Id) || string.IsNullOrWhiteSpace(approvalUrl))
        {
            _logger.LogWarning("PayPal create order response missing approval link.");
            return PayPalCreateOrderResult.Fail("PayPal returned an invalid checkout response.");
        }

        return PayPalCreateOrderResult.Ok(order.Id, approvalUrl);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return PayPalCaptureResult.Fail(GetNotConfiguredMessage());
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return PayPalCaptureResult.Fail("Unable to connect to PayPal.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.ApiBaseUrl}/v2/checkout/orders/{payPalOrderId}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal capture request failed for order {PayPalOrderId}.", payPalOrderId);
            return PayPalCaptureResult.Fail("Payment capture failed. Please try again.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var capture = ParseCapture(body);
            if (capture is null)
            {
                return PayPalCaptureResult.Fail("PayPal capture response was invalid.");
            }

            return PayPalCaptureResult.Ok(capture.Value.CaptureId, capture.Value.Amount);
        }

        if (body.Contains("ORDER_ALREADY_CAPTURED", StringComparison.OrdinalIgnoreCase)
            || body.Contains("ORDER_ALREADY_COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            var capture = await GetCapturedOrderDetailsAsync(payPalOrderId, token, cancellationToken);
            if (capture is null)
            {
                return PayPalCaptureResult.AlreadyDone(null, 0);
            }

            return PayPalCaptureResult.AlreadyDone(capture.Value.CaptureId, capture.Value.Amount);
        }

        _logger.LogWarning(
            "PayPal capture failed for order {PayPalOrderId} with status {StatusCode}.",
            payPalOrderId,
            response.StatusCode);

        return PayPalCaptureResult.Fail("Payment could not be completed. Please try again.");
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<string>("paypal:access_token", out var cachedToken))
        {
            return cachedToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.ApiBaseUrl}/v1/oauth2/token");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal OAuth token request failed.");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal OAuth token request failed with status {StatusCode}.", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(body, JsonOptions);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            return null;
        }

        var expiresIn = Math.Max(60, tokenResponse.ExpiresIn - 60);
        _cache.Set("paypal:access_token", tokenResponse.AccessToken, TimeSpan.FromSeconds(expiresIn));
        return tokenResponse.AccessToken;
    }

    private async Task<(string CaptureId, decimal Amount)?> GetCapturedOrderDetailsAsync(
        string payPalOrderId,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_settings.ApiBaseUrl}/v2/checkout/orders/{payPalOrderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCapture(body);
    }

    private static (string CaptureId, decimal Amount)? ParseCapture(string body)
    {
        var order = JsonSerializer.Deserialize<PayPalOrderResponse>(body, JsonOptions);
        var capture = order?.PurchaseUnits?
            .SelectMany(unit => unit.Payments?.Captures ?? [])
            .FirstOrDefault(item => item is not null);

        if (capture is null || string.IsNullOrWhiteSpace(capture.Id))
        {
            return null;
        }

        if (!decimal.TryParse(
                capture.Amount?.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        return (capture.Id, amount);
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);

    private string GetNotConfiguredMessage() =>
        _environment.IsDevelopment()
            ? "PayPal sandbox chưa được cấu hình. Vào developer.paypal.com lấy Client ID/Secret, rồi chạy: dotnet user-secrets set \"PayPal:ClientId\" \"...\" và dotnet user-secrets set \"PayPal:ClientSecret\" \"...\". Restart app sau khi set (môi trường phải là Development)."
            : "PayPal is not configured. Contact support.";

    private sealed class PayPalTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }
    }

    private sealed class PayPalOrderResponse
    {
        public string Id { get; set; } = string.Empty;

        public List<PayPalLink> Links { get; set; } = [];

        public List<PayPalPurchaseUnit> PurchaseUnits { get; set; } = [];
    }

    private sealed class PayPalLink
    {
        public string Href { get; set; } = string.Empty;

        public string Rel { get; set; } = string.Empty;
    }

    private sealed class PayPalPurchaseUnit
    {
        public PayPalPayments? Payments { get; set; }
    }

    private sealed class PayPalPayments
    {
        public List<PayPalCapture>? Captures { get; set; }
    }

    private sealed class PayPalCapture
    {
        public string Id { get; set; } = string.Empty;

        public PayPalMoney? Amount { get; set; }
    }

    private sealed class PayPalMoney
    {
        public string Value { get; set; } = string.Empty;
    }
}
