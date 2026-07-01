namespace OnlineAuction.Messaging.Messages;

public sealed class NotificationDeliverMessage
{
    public int NotificationId { get; init; }

    public int UserId { get; init; }
}
