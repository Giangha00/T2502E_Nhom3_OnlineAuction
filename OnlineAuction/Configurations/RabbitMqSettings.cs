using RabbitMQ.Client;

namespace OnlineAuction.Configurations;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; } = true;

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Enable TLS (AMQPS). Required for CloudAMQP / managed brokers on port 5671.</summary>
    public bool UseSsl { get; set; }

    public string ExchangeName { get; set; } = "online-auction.direct";

    /// <summary>Max unacked messages per consumer channel — keeps memory bounded.</summary>
    public ushort PrefetchCount { get; set; } = 10;

    public ConnectionFactory CreateConnectionFactory()
    {
        var factory = new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        if (UseSsl)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = HostName
            };
        }

        return factory;
    }
}
