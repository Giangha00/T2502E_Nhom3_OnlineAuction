using OnlineAuction.Services.Interfaces;

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
                var lifecycleQueue = scope.ServiceProvider.GetRequiredService<IAuctionLifecycleQueueService>();
                await lifecycleQueue.PublishTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish auction lifecycle work.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
