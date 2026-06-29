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

    public string ExchangeName { get; set; } = "online-auction.direct";

    /// <summary>Max unacked messages per consumer channel — keeps memory bounded.</summary>
    public ushort PrefetchCount { get; set; } = 10;
}
