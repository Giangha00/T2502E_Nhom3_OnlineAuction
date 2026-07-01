using OnlineAuction.Messaging;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Messaging.Handlers;

namespace OnlineAuction.Services;

/// <summary>
/// Publishes auction lifecycle work to RabbitMQ, with inline fallback when MQ is unavailable.
/// </summary>
public interface IAuctionLifecycleQueueService
{
    Task PublishTickAsync(CancellationToken cancellationToken = default);
}

public sealed class AuctionLifecycleQueueService : IAuctionLifecycleQueueService
{
    private readonly IRabbitMqPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuctionLifecycleQueueService(
        IRabbitMqPublisher publisher,
        IServiceScopeFactory scopeFactory)
    {
        _publisher = publisher;
        _scopeFactory = scopeFactory;
    }

    public async Task PublishTickAsync(CancellationToken cancellationToken = default)
    {
        var actions = new[]
        {
            AuctionLifecycleAction.FinalizeExpiredAuctions,
            AuctionLifecycleAction.CancelExpiredOrders,
            AuctionLifecycleAction.ProcessEndingSoonNotifications,
            AuctionLifecycleAction.ProcessStartingSoonNotifications,
            AuctionLifecycleAction.ActivateScheduledAuctions
        };

        foreach (var action in actions)
        {
            var message = new AuctionLifecycleMessage { Action = action };
            if (_publisher.TryPublish(RabbitMqQueueNames.AuctionLifecycle, message))
            {
                continue;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IAuctionLifecycleMessageHandler>();
            await handler.HandleAsync(message, cancellationToken);
        }
    }
}
