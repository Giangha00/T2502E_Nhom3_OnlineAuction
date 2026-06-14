using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockNotificationData
{
    public static List<NotificationItemViewModel> GetNotifications() =>
    [
        new()
        {
            Id = 1,
            Type = NotificationType.Auction,
            Title = "New bid received",
            Description = "Someone placed a higher bid on your Vintage Watch.",
            FullMessage = "Your product Vintage Watch has received a new bid. The current leading bid is now $1,250. Review the auction activity and decide whether you want to update your reserve strategy.",
            TimeAgo = "5 minutes ago",
            CreatedAt = new DateTime(2026, 6, 10, 14, 30, 0),
            IsRead = false,
            RelatedObjectLabel = "Vintage Watch auction",
            RelatedUrl = "/Auction/Detail/1",
            ActionLabel = "View Auction"
        },
        new()
        {
            Id = 2,
            Type = NotificationType.Auction,
            Title = "Auction ending soon",
            Description = "Classic Camera auction ends in less than 1 hour.",
            FullMessage = "The Classic Camera auction is entering its final hour. Check the current bid and auction status before the closing time.",
            TimeAgo = "28 minutes ago",
            CreatedAt = new DateTime(2026, 6, 10, 14, 7, 0),
            IsRead = false,
            RelatedObjectLabel = "Classic Camera auction",
            RelatedUrl = "/Auction/Detail/2",
            ActionLabel = "View Auction"
        },
        new()
        {
            Id = 3,
            Type = NotificationType.Winning,
            Title = "You won this auction",
            Description = "Congratulations. You won the Antique Lamp auction.",
            FullMessage = "Congratulations. You placed the winning bid for Antique Lamp. Please complete payment before the deadline to secure your order.",
            TimeAgo = "1 hour ago",
            CreatedAt = new DateTime(2026, 6, 10, 13, 20, 0),
            IsRead = false,
            RelatedObjectLabel = "Antique Lamp order",
            RelatedUrl = "/Payment/Checkout?auctionId=3",
            ActionLabel = "Pay Now"
        },
        new()
        {
            Id = 4,
            Type = NotificationType.Payment,
            Title = "Payment completed",
            Description = "Your payment for Designer Chair was completed successfully.",
            FullMessage = "Payment completed successfully for Designer Chair. Your transaction has been recorded and the seller will prepare the item for delivery.",
            TimeAgo = "2 hours ago",
            CreatedAt = new DateTime(2026, 6, 10, 12, 15, 0),
            IsRead = true,
            RelatedObjectLabel = "Payment AH-20260610-0004",
            RelatedUrl = "/Payment/Confirmation?orderRef=AH-20260610-0004&auctionName=Designer%20Chair&total=980&method=Credit%20Card",
            ActionLabel = "View Payment"
        },
        new()
        {
            Id = 5,
            Type = NotificationType.Refund,
            Title = "Refund approved",
            Description = "Your refund request has been approved.",
            FullMessage = "Your refund request for order AH-20260608-0011 has been approved. The refund will be processed to your original payment method.",
            TimeAgo = "Yesterday",
            CreatedAt = new DateTime(2026, 6, 9, 16, 45, 0),
            IsRead = true,
            RelatedObjectLabel = "Refund request AH-20260608-0011",
            RelatedUrl = "/Refund",
            ActionLabel = "View Refund"
        },
        new()
        {
            Id = 6,
            Type = NotificationType.System,
            Title = "System maintenance update",
            Description = "Auction House maintenance is scheduled for this weekend.",
            FullMessage = "Auction House will perform scheduled maintenance on Sunday from 02:00 to 04:00. Bidding and checkout may be briefly unavailable during this period.",
            TimeAgo = "2 days ago",
            CreatedAt = new DateTime(2026, 6, 8, 9, 0, 0),
            IsRead = false,
            RelatedObjectLabel = "System announcement",
            RelatedUrl = "/Faq",
            ActionLabel = "Learn More"
        }
    ];

    public static NotificationItemViewModel? GetById(int id) =>
        GetNotifications().FirstOrDefault(notification => notification.Id == id);
}
