using RabbitMQ.Client;

namespace OnlineAuction.Messaging;

internal static class RabbitMqTopology
{
    public static void Declare(IModel channel, string exchangeName)
    {
        channel.ExchangeDeclare(
            exchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        DeclareQueue(channel, exchangeName, RabbitMqQueueNames.BidsPlaced);
        DeclareQueue(channel, exchangeName, RabbitMqQueueNames.NotificationsDeliver);
        DeclareQueue(channel, exchangeName, RabbitMqQueueNames.EmailsSend);
        DeclareQueue(channel, exchangeName, RabbitMqQueueNames.AuctionLifecycle);
    }

    private static void DeclareQueue(IModel channel, string exchangeName, string queueName)
    {
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(queueName, exchangeName, queueName);
    }
}
