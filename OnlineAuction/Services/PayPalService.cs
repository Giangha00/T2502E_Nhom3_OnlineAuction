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
        PropertyNameCaseInsensitive = true,
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
            _logger.LogWarning(
                "PayPal create order failed with status {StatusCode}. Body={Body}",
                response.StatusCode,
                TruncateForLog(body));
            return PayPalCreateOrderResult.Fail("PayPal could not start checkout. Please try again.");
        }

        var order = JsonSerializer.Deserialize<PayPalOrderResponse>(body, JsonOptions);
        var approvalUrl = order?.Links?
            .FirstOrDefault(link =>
                link.Rel.Equals("approve", StringComparison.OrdinalIgnoreCase)
                || link.Rel.Equals("payer-action", StringComparison.OrdinalIgnoreCase))
            ?.Href;

        if (string.IsNullOrWhiteSpace(order?.Id) || string.IsNullOrWhiteSpace(approvalUrl))
        {
            _logger.LogWarning(
                "PayPal create order response missing approval link. Body={Body}",
                TruncateForLog(body));
            return PayPalCreateOrderResult.Fail("PayPal returned an invalid checkout response.");
        }

        return PayPalCreateOrderResult.Ok(order.Id, approvalUrl);
    }

    public async Task<PayPalOrderDetailsResult> GetOrderDetailsAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return PayPalOrderDetailsResult.Fail(GetNotConfiguredMessage());
        }

        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return PayPalOrderDetailsResult.Fail("Missing PayPal order id.");
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return PayPalOrderDetailsResult.Fail("Unable to connect to PayPal.");
        }

        var parsed = await FetchOrderDetailsAsync(payPalOrderId, token, cancellationToken);
        if (parsed is null)
        {
            return PayPalOrderDetailsResult.Fail("Unable to load PayPal order details.");
        }

        return PayPalOrderDetailsResult.Ok(
            payPalOrderId,
            parsed.Value.Status,
            parsed.Value.OrderAmount,
            parsed.Value.CaptureId,
            parsed.Value.CapturedAmount);
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
                // Some capture payloads omit purchase_units[].amount; fall back to GET order.
                _logger.LogWarning(
                    "PayPal capture parse failed for order {PayPalOrderId}. Falling back to order details. Body={Body}",
                    payPalOrderId,
                    TruncateForLog(body));

                var details = await FetchOrderDetailsAsync(payPalOrderId, token, cancellationToken);
                if (details is not null && !string.IsNullOrWhiteSpace(details.Value.CaptureId))
                {
                    return PayPalCaptureResult.Ok(
                        details.Value.CaptureId,
                        details.Value.CapturedAmount ?? details.Value.OrderAmount);
                }

                return PayPalCaptureResult.Fail("PayPal capture response was invalid.");
            }

            return PayPalCaptureResult.Ok(capture.Value.CaptureId, capture.Value.Amount);
        }

        if (body.Contains("ORDER_ALREADY_CAPTURED", StringComparison.OrdinalIgnoreCase)
            || body.Contains("ORDER_ALREADY_COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            var capture = await FetchOrderDetailsAsync(payPalOrderId, token, cancellationToken);
            if (capture is null || string.IsNullOrWhiteSpace(capture.Value.CaptureId))
            {
                return PayPalCaptureResult.AlreadyDone(null, 0);
            }

            return PayPalCaptureResult.AlreadyDone(
                capture.Value.CaptureId,
                capture.Value.CapturedAmount ?? capture.Value.OrderAmount);
        }

        _logger.LogWarning(
            "PayPal capture failed for order {PayPalOrderId} with status {StatusCode}.",
            payPalOrderId,
            response.StatusCode);

        return PayPalCaptureResult.Fail("Payment could not be completed. Please try again.");
    }

    public async Task<PayPalCancelResult> CancelOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return PayPalCancelResult.Fail(GetNotConfiguredMessage());
        }

        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return PayPalCancelResult.Fail("Missing PayPal order id.");
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return PayPalCancelResult.Fail("Unable to connect to PayPal.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.ApiBaseUrl}/v2/checkout/orders/{payPalOrderId}/void");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayPal cancel order request failed for order {PayPalOrderId}.", payPalOrderId);
            return PayPalCancelResult.Fail("Unable to cancel PayPal order.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return PayPalCancelResult.Ok();
        }

        if (body.Contains("ORDER_ALREADY_CAPTURED", StringComparison.OrdinalIgnoreCase)
            || body.Contains("ORDER_ALREADY_COMPLETED", StringComparison.OrdinalIgnoreCase)
            || body.Contains("ORDER_ALREADY_VOIDED", StringComparison.OrdinalIgnoreCase))
        {
            return PayPalCancelResult.Fail("PayPal order cannot be cancelled because it has already been completed.");
        }

        _logger.LogWarning(
            "PayPal cancel order failed for order {PayPalOrderId} with status {StatusCode}. Body={Body}",
            payPalOrderId,
            response.StatusCode,
            body);

        return PayPalCancelResult.Fail("Unable to cancel PayPal order.");
    }

    public async Task<PayPalVerifyWebhookResult> VerifyWebhookSignatureAsync(
        string requestBody,
        IHeaderDictionary headers,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return PayPalVerifyWebhookResult.Fail(GetNotConfiguredMessage());
        }

        if (string.IsNullOrWhiteSpace(_settings.WebhookId))
        {
            return PayPalVerifyWebhookResult.Fail("PayPal webhook id is not configured.");
        }

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return PayPalVerifyWebhookResult.Fail("Empty webhook payload.");
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return PayPalVerifyWebhookResult.Fail("Unable to connect to PayPal.");
        }

        if (!headers.TryGetValue("Paypal-Transmission-Id", out var transmissionId)
            || !headers.TryGetValue("Paypal-Transmission-Time", out var transmissionTime)
            || !headers.TryGetValue("Paypal-Transmission-Sig", out var transmissionSig)
            || !headers.TryGetValue("Paypal-Cert-Url", out var certUrl)
            || !headers.TryGetValue("Paypal-Auth-Algo", out var authAlgo))
        {
            return PayPalVerifyWebhookResult.Fail("Missing PayPal webhook verification headers.");
        }

        JsonElement webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<JsonElement>(requestBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid PayPal webhook payload.");
            return PayPalVerifyWebhookResult.Fail("Invalid PayPal webhook payload.");
        }

        var payload = new
        {
            auth_algo = authAlgo.ToString(),
            cert_url = certUrl.ToString(),
            transmission_id = transmissionId.ToString(),
            transmission_sig = transmissionSig.ToString(),
            transmission_time = transmissionTime.ToString(),
            webhook_id = _settings.WebhookId,
            webhook_event = webhookEvent
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.ApiBaseUrl}/v1/notifications/verify-webhook-signature");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal verify webhook signature request failed.");
            return PayPalVerifyWebhookResult.Fail("Unable to verify PayPal webhook.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "PayPal webhook verification failed with status {StatusCode}. Body={Body}",
                response.StatusCode,
                responseBody);
            return PayPalVerifyWebhookResult.Fail("PayPal webhook verification failed.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var verificationResponse = JsonSerializer.Deserialize<PayPalWebhookVerificationResponse>(body, JsonOptions);
        if (verificationResponse is null ||
            !verificationResponse.VerificationStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "PayPal webhook signature not verified. Response={Response}",
                body);
            return PayPalVerifyWebhookResult.Fail("PayPal webhook signature could not be verified.");
        }

        return PayPalVerifyWebhookResult.Ok();
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

    private async Task<(string Status, decimal OrderAmount, string? CaptureId, decimal? CapturedAmount)?> FetchOrderDetailsAsync(
        string payPalOrderId,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_settings.ApiBaseUrl}/v2/checkout/orders/{payPalOrderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal get order details failed for order {PayPalOrderId}.", payPalOrderId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "PayPal get order details failed for order {PayPalOrderId} with status {StatusCode}.",
                payPalOrderId,
                response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseOrderDetails(body);
    }

    private static (string Status, decimal OrderAmount, string? CaptureId, decimal? CapturedAmount)? ParseOrderDetails(string body)
    {
        var order = JsonSerializer.Deserialize<PayPalOrderResponse>(body, JsonOptions);
        if (order is null || string.IsNullOrWhiteSpace(order.Status))
        {
            return null;
        }

        var purchaseUnit = order.PurchaseUnits?.FirstOrDefault();
        var capture = purchaseUnit?.Payments?.Captures?
            .FirstOrDefault(item => item is not null && !string.IsNullOrWhiteSpace(item.Id));

        decimal? capturedAmount = null;
        if (capture?.Amount?.Value is not null
            && decimal.TryParse(
                capture.Amount.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedCapturedAmount))
        {
            capturedAmount = parsedCapturedAmount;
        }

        decimal? orderAmount = null;
        if (purchaseUnit?.Amount?.Value is not null
            && decimal.TryParse(
                purchaseUnit.Amount.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedOrderAmount))
        {
            orderAmount = parsedOrderAmount;
        }

        // Capture responses sometimes omit purchase_units[].amount and only include
        // payments.captures[].amount — accept either source.
        var resolvedAmount = orderAmount ?? capturedAmount;
        if (resolvedAmount is null)
        {
            return null;
        }

        if (capture is null || string.IsNullOrWhiteSpace(capture.Id))
        {
            return (order.Status, resolvedAmount.Value, null, null);
        }

        return (order.Status, resolvedAmount.Value, capture.Id, capturedAmount ?? resolvedAmount);
    }

    private static (string CaptureId, decimal Amount)? ParseCapture(string body)
    {
        var parsed = ParseOrderDetails(body);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Value.CaptureId))
        {
            return null;
        }

        return (parsed.Value.CaptureId, parsed.Value.CapturedAmount ?? parsed.Value.OrderAmount);
    }

    private static string TruncateForLog(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength] + "…";
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);

    private string GetNotConfiguredMessage() =>
        _environment.IsDevelopment()
            ? "PayPal sandbox chưa được cấu hình. Vào developer.paypal.com lấy Client ID/Secret, rồi chạy: dotnet user-secrets set \"PayPal:ClientId\" \"...\" và dotnet user-secrets set \"PayPal:ClientSecret\" \"...\". Restart app sau khi set (môi trường phải là Development)."
            : "PayPal is not configured. Contact support.";

    private sealed class PayPalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class PayPalOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("links")]
        public List<PayPalLink> Links { get; set; } = [];

        [JsonPropertyName("purchase_units")]
        public List<PayPalPurchaseUnit> PurchaseUnits { get; set; } = [];
    }

    private sealed class PayPalLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("rel")]
        public string Rel { get; set; } = string.Empty;
    }

    private sealed class PayPalPurchaseUnit
    {
        [JsonPropertyName("amount")]
        public PayPalMoney? Amount { get; set; }

        [JsonPropertyName("payments")]
        public PayPalPayments? Payments { get; set; }
    }

    private sealed class PayPalPayments
    {
        [JsonPropertyName("captures")]
        public List<PayPalCapture>? Captures { get; set; }
    }

    private sealed class PayPalCapture
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public PayPalMoney? Amount { get; set; }
    }

    private sealed class PayPalMoney
    {
        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PayPalWebhookVerificationResponse
    {
        [JsonPropertyName("verification_status")]
        public string VerificationStatus { get; set; } = string.Empty;
    }

    public async Task<PayPalRefundResult> RefundCaptureAsync(
    string captureId,
    decimal amount,
    CancellationToken cancellationToken = default)
{
    // Kiểm tra captureId có hợp lệ không
    if (string.IsNullOrWhiteSpace(captureId))
    {
        return PayPalRefundResult.Fail("Missing PayPal capture id.");
    }

    // Số tiền hoàn phải lớn hơn 0
    if (amount <= 0)
    {
        return PayPalRefundResult.Fail("Refund amount must be greater than 0.");
    }

    // Lấy access token PayPal
    var accessToken = await GetAccessTokenAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return PayPalRefundResult.Fail("Unable to get PayPal access token.");
    }

    // Body gửi lên PayPal để refund đúng số tiền
    var payload = new
    {
        amount = new
        {
            currency_code = _settings.CurrencyCode,
            value = FormatAmount(amount)
        }
    };

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"{_settings.ApiBaseUrl}/v2/payments/captures/{captureId}/refund");

    request.Headers.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    request.Content = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(payload, JsonOptions),
        System.Text.Encoding.UTF8,
        "application/json");

    HttpResponseMessage response;

    try
    {
        response = await _httpClient.SendAsync(request, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "PayPal refund request failed. CaptureId={CaptureId}", captureId);

        return PayPalRefundResult.Fail("Không thể kết nối PayPal để hoàn tiền.");
    }

    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "PayPal refund failed. CaptureId={CaptureId}, StatusCode={StatusCode}, Body={Body}",
            captureId,
            response.StatusCode,
            body);

        return PayPalRefundResult.Fail("PayPal refund thất bại.");
    }

    using var document = System.Text.Json.JsonDocument.Parse(body);
    var root = document.RootElement;

    var refundId = root.TryGetProperty("id", out var idElement)
        ? idElement.GetString()
        : null;

    var status = root.TryGetProperty("status", out var statusElement)
        ? statusElement.GetString()
        : "UNKNOWN";

    if (string.IsNullOrWhiteSpace(refundId))
    {
        return PayPalRefundResult.Fail("PayPal refund response không có refund id.");
    }

    return PayPalRefundResult.Ok(refundId, status ?? "UNKNOWN");
}
}
