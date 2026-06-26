using OnlineAuction.Services.Interfaces;
using OnlineAuction.Areas.Admin.Services;

namespace OnlineAuction.Services;

public class AuctionFinalizationWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionFinalizationWorker> _logger;

    public AuctionFinalizationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionFinalizationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderCreationService = scope.ServiceProvider.GetRequiredService<IOrderCreationService>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var verificationService = scope.ServiceProvider.GetRequiredService<IAdminAuctionVerificationService>();

                var createdCount = await orderCreationService.FinalizeExpiredAuctionsAsync(stoppingToken);
                var cancelledCount = await orderService.CancelAllExpiredPendingOrdersAsync();
                await notificationService.ProcessAuctionEndingSoonNotificationsAsync(stoppingToken);
                var activatedCount = await verificationService.ActivateScheduledAuctionsAsync(stoppingToken);

                if (createdCount > 0)
                {
                    _logger.LogInformation("Created {CreatedCount} pending payment auction orders.", createdCount);
                }

                if (cancelledCount > 0)
                {
                    _logger.LogInformation("Cancelled {CancelledCount} expired pending payment orders.", cancelledCount);
                }

                if (activatedCount > 0)
                {
                    _logger.LogInformation("Activated {ActivatedCount} scheduled auctions.", activatedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize expired auctions.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
