namespace OnlineAuction.Messaging.Messages;

public enum AuctionLifecycleAction
{
    FinalizeExpiredAuctions = 1,
    CancelExpiredOrders = 2,
    ProcessEndingSoonNotifications = 3,
    ActivateScheduledAuctions = 4,
    AuctionEnded = 5,
    AuctionEndingSoon = 6
}

public sealed class AuctionLifecycleMessage
{
    public AuctionLifecycleAction Action { get; init; }

    public int? AuctionId { get; init; }
}
