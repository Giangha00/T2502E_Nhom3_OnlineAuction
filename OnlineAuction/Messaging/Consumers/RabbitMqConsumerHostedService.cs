using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Messaging.Handlers;
using OnlineAuction.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OnlineAuction.Messaging.Consumers;

/// <summary>
/// Hosts all RabbitMQ consumers on a single shared connection (memory-efficient).
/// Each queue gets its own channel with bounded prefetch.
/// </summary>
public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private IConnection? _connection;
    private readonly List<IModel> _channels = [];

    public RabbitMqConsumerHostedService(
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        _settings = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("RabbitMQ consumers disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer connection failed. Retrying in 5 seconds.");
                Cleanup();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        Cleanup();
    }

    private Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = factory.CreateConnection("online-auction-consumers");
        _logger.LogInformation("RabbitMQ consumers connected to {Host}:{Port}.", _settings.HostName, _settings.Port);

        using (var setupChannel = _connection.CreateModel())
        {
            RabbitMqTopology.Declare(setupChannel, _settings.ExchangeName);
        }

        RegisterConsumer(RabbitMqQueueNames.BidsPlaced, HandleBidPlacedAsync);
        RegisterConsumer(RabbitMqQueueNames.NotificationsDeliver, HandleNotificationDeliverAsync);
        RegisterConsumer(RabbitMqQueueNames.EmailsSend, HandleEmailSendAsync, requeueOnFailure: false);
        RegisterConsumer(RabbitMqQueueNames.AuctionLifecycle, HandleAuctionLifecycleAsync);

        stoppingToken.Register(Cleanup);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void RegisterConsumer(
        string queueName,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler,
        bool requeueOnFailure = true)
    {
        RegisterConsumerCore(
            queueName,
            async (body, cancellationToken) =>
            {
                await handler(body, cancellationToken);
                return true;
            },
            requeueOnFailure);
    }

    private void RegisterConsumer(
        string queueName,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task<bool>> handler,
        bool requeueOnFailure = true)
    {
        RegisterConsumerCore(queueName, handler, requeueOnFailure);
    }

    private void RegisterConsumerCore(
        string queueName,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task<bool>> handler,
        bool requeueOnFailure)
    {
        if (_connection is null)
        {
            return;
        }

        var channel = _connection.CreateModel();
        _channels.Add(channel);

        channel.BasicQos(0, _settings.PrefetchCount, false);
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var succeeded = await handler(eventArgs.Body, CancellationToken.None);
                if (succeeded)
                {
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                    return;
                }

                _logger.LogWarning(
                    "Message from queue {QueueName} was not processed successfully.",
                    queueName);
                channel.BasicNack(eventArgs.DeliveryTag, false, requeue: requeueOnFailure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message from queue {QueueName}.", queueName);
                channel.BasicNack(eventArgs.DeliveryTag, false, requeue: requeueOnFailure);
            }
        };

        channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("RabbitMQ consumer listening on queue {QueueName}.", queueName);
    }

    private async Task HandleBidPlacedAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var message = RabbitMqJson.Deserialize<BidPlacedMessage>(body.Span);
        if (message is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBidPlacedMessageHandler>();
        await handler.HandleAsync(message, cancellationToken);
    }

    private async Task HandleNotificationDeliverAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var message = RabbitMqJson.Deserialize<NotificationDeliverMessage>(body.Span);
        if (message is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var delivery = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
        await delivery.DeliverAsync(message.NotificationId, cancellationToken);
    }

    private async Task<bool> HandleEmailSendAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var message = RabbitMqJson.Deserialize<EmailSendMessage>(body.Span);
        if (message is null)
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEmailSendMessageHandler>();
        return await handler.HandleAsync(message, cancellationToken);
    }

    private async Task HandleAuctionLifecycleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var message = RabbitMqJson.Deserialize<AuctionLifecycleMessage>(body.Span);
        if (message is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IAuctionLifecycleMessageHandler>();
        await handler.HandleAsync(message, cancellationToken);
    }

    private void Cleanup()
    {
        foreach (var channel in _channels)
        {
            try
            {
                channel.Close();
                channel.Dispose();
            }
            catch
            {
                // ignored during shutdown
            }
        }

        _channels.Clear();

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch
        {
            // ignored during shutdown
        }

        _connection = null;
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }
}
