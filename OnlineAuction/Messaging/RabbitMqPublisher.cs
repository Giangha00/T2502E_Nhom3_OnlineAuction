using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using RabbitMQ.Client;

namespace OnlineAuction.Messaging;

public interface IRabbitMqPublisher
{
    bool IsEnabled { get; }

    bool TryPublish<T>(string routingKey, T message);
}

/// <summary>
/// Lightweight publisher: one shared connection, short-lived channels per publish to avoid channel leaks.
/// </summary>
public sealed class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private bool _topologyDeclared;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public bool TryPublish<T>(string routingKey, T message)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        try
        {
            using var channel = CreateChannel();
            EnsureTopology(channel);

            var body = RabbitMqJson.Serialize(message);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;

            channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish RabbitMQ message to {RoutingKey}.", routingKey);
            return false;
        }
    }

    private IConnection GetOrCreateConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            _topologyDeclared = false;

            var factory = _settings.CreateConnectionFactory();
            _connection = factory.CreateConnection("online-auction-publisher");
            _logger.LogInformation(
                "RabbitMQ publisher connected to {Host}:{Port} (ssl={UseSsl}).",
                _settings.HostName,
                _settings.Port,
                _settings.UseSsl);

            return _connection;
        }
    }

    private IModel CreateChannel()
    {
        var connection = GetOrCreateConnection();
        return connection.CreateModel();
    }

    private void EnsureTopology(IModel channel)
    {
        if (_topologyDeclared)
        {
            return;
        }

        lock (_connectionLock)
        {
            if (_topologyDeclared)
            {
                return;
            }

            RabbitMqTopology.Declare(channel, _settings.ExchangeName);
            _topologyDeclared = true;
        }
    }

    public void Dispose()
    {
        lock (_connectionLock)
        {
            _connection?.Dispose();
            _connection = null;
            _topologyDeclared = false;
        }
    }
}
