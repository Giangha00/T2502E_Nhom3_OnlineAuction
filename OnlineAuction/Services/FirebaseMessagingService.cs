using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class FirebaseMessagingService : IFcmService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly FirebaseSettings _settings;
    private readonly ILogger<FirebaseMessagingService> _logger;

    public FirebaseMessagingService(
        AuctionHouseDbContext dbContext,
        IOptions<FirebaseSettings> settings,
        ILogger<FirebaseMessagingService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        int userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string> dataPayload,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsAdminConfigured || FirebaseApp.DefaultInstance is null)
        {
            return;
        }

        var tokens = await _dbContext.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.FcmToken)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        var messaging = FirebaseMessaging.DefaultInstance;
        if (messaging is null)
        {
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            },
            Data = dataPayload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        BatchResponse response;
        try
        {
            response = await messaging.SendEachForMulticastAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM multicast send failed for user {UserId}.", userId);
            return;
        }

        if (response.FailureCount == 0)
        {
            return;
        }

        var invalidTokens = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var sendResponse = response.Responses[i];
            if (sendResponse.IsSuccess)
            {
                continue;
            }

            var errorCode = sendResponse.Exception?.MessagingErrorCode;
            if (errorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                invalidTokens.Add(tokens[i]);
            }
            else
            {
                _logger.LogDebug(
                    sendResponse.Exception,
                    "FCM send failed for token index {Index}, user {UserId}.",
                    i,
                    userId);
            }
        }

        if (invalidTokens.Count == 0)
        {
            return;
        }

        var stale = await _dbContext.UserDeviceTokens
            .Where(t => invalidTokens.Contains(t.FcmToken))
            .ToListAsync(cancellationToken);

        if (stale.Count > 0)
        {
            _dbContext.UserDeviceTokens.RemoveRange(stale);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} invalid FCM tokens.", stale.Count);
        }
    }

    public static void Initialize(FirebaseSettings settings, ILogger logger)
    {
        if (!settings.IsAdminConfigured)
        {
            logger.LogInformation("Firebase Admin SDK not configured — push notifications disabled.");
            return;
        }

        if (FirebaseApp.DefaultInstance is not null)
        {
            return;
        }

        try
        {
            var privateKey = NormalizePrivateKey(settings.PrivateKey);
            var credential = GoogleCredential.FromServiceAccountCredential(new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(settings.ClientEmail)
                {
                    ProjectId = settings.ProjectId
                }.FromPrivateKey(privateKey)));

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = settings.ProjectId
            });

            logger.LogInformation("Firebase Admin SDK initialized for project {ProjectId}.", settings.ProjectId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Firebase Admin SDK.");
        }
    }

    private static string NormalizePrivateKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return string.Empty;
        }

        var key = rawKey.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
        const string beginMarker = "-----BEGIN PRIVATE KEY-----";
        const string endMarker = "-----END PRIVATE KEY-----";

        var beginIndex = key.IndexOf(beginMarker, StringComparison.Ordinal);
        var endIndex = key.IndexOf(endMarker, StringComparison.Ordinal);

        if (beginIndex >= 0 && endIndex > beginIndex)
        {
            endIndex += endMarker.Length;
            return key[beginIndex..endIndex];
        }

        return key;
    }
}
