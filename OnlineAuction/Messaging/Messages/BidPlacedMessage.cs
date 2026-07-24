namespace OnlineAuction.Messaging.Messages;

/// <summary>
/// Published after a bid is committed to the database.
/// Consumer verifies bidder/amount against current DB state before pushing realtime updates.
/// </summary>
public sealed class BidPlacedMessage
{
    public int AuctionId { get; init; }

    public long BidId { get; init; }

    public int BidderId { get; init; }

    public int SellerId { get; init; }

    public decimal Amount { get; init; }

    public decimal PreviousPrice { get; init; }

    public IReadOnlyList<int> OutbidUserIds { get; init; } = [];

    public string ProductName { get; init; } = string.Empty;
}
