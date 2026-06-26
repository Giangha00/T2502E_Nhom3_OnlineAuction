using System.Net.Http.Json;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class EmailVerificationService : IEmailVerificationService
{
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
        var endpoint = _configuration["EmailVerification:CloudEndpoint"];
        var apiKey = _configuration["EmailVerification:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("EmailVerification config is missing.");
            return false;
        }

        var payload = new
        {
            to,
            fullName,
            confirmUrl,
            locale
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("X-API-KEY", apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Send verification email failed. StatusCode: {StatusCode}", response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud email service error.");
            return false;
        }
    }
}