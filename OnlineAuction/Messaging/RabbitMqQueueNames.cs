namespace OnlineAuction.Messaging;

public static class RabbitMqQueueNames
{
    public const string BidsPlaced = "bids.placed";
    public const string NotificationsDeliver = "notifications.deliver";
    public const string EmailsSend = "emails.send";
    public const string AuctionLifecycle = "auction.lifecycle";
}
