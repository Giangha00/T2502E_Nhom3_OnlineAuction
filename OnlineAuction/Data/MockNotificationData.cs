using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockNotificationData
{
    public static List<NotificationItemViewModel> GetNotifications() =>
    [
        new()
        {
            Id = 1,
            Title = "You've been outbid",
            Message = "Someone placed a higher bid on Charizard 1st Edition Holo.",
            TimeAgo = "5 min ago",
            Type = NotificationType.Auction,
            RelatedUrl = "/Auction/Detail/1"
        },
        new()
        {
            Id = 2,
            Title = "Auction ending soon",
            Message = "Pikachu Illustrator ends in 2 hours. Place your final bid now.",
            TimeAgo = "1 hour ago",
            Type = NotificationType.Auction,
            RelatedUrl = "/Auction/Detail/2"
        },
        new()
        {
            Id = 3,
            Title = "You won the auction!",
            Message = "Congratulations! You won the Lugia Neo Genesis PSA 10 auction.",
            TimeAgo = "3 hours ago",
            Type = NotificationType.Winning,
            RelatedUrl = "/Payment/Checkout"
        },
        new()
        {
            Id = 4,
            Title = "Payment confirmed",
            Message = "Your payment for Designer Chair has been processed successfully.",
            TimeAgo = "Yesterday",
            Type = NotificationType.Payment,
            RelatedUrl = "/Payment/Confirmation"
        },
        new()
        {
            Id = 5,
            Title = "Refund approved",
            Message = "Your refund request #RF-1042 has been approved and will arrive in 3–5 days.",
            TimeAgo = "2 days ago",
            Type = NotificationType.Refund,
            RelatedUrl = "/Refund/Confirmation"
        },
        new()
        {
            Id = 6,
            Title = "Account security update",
            Message = "We've enhanced our two-factor authentication options for your account.",
            TimeAgo = "1 week ago",
            Type = NotificationType.System,
            RelatedUrl = "/Faq"
        }
    ];
}
