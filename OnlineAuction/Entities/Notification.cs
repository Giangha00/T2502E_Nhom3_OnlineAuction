using OnlineAuction.Models;

namespace OnlineAuction.Entities;

public class Notification : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = NotificationType.System.ToString().ToLowerInvariant();

    public string? RelatedUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

public static class NotificationReferenceTypes
{
    public const string AuctionOutbid = "auction_outbid";
    public const string AuctionEndingSoon = "auction_ending_soon";
    public const string AuctionWon = "auction_won";
    public const string PaymentSuccess = "payment_success";
    public const string RefundApproved = "refund_approved";
    public const string RefundRequested = "refund_requested";
    public const string RefundRejected = "refund_rejected";
    public const string AuctionRegistrationConfirmed = "auction_registration_confirmed";
    public const string AuctionRegistrationCancelled = "auction_registration_cancelled";
    public const string AuctionDepositRefunded = "auction_deposit_refunded";
    public const string AuctionStartingSoon = "auction_starting_soon";
    public const string AuctionNowLive = "auction_now_live";
    public const string ListingFeePaid = "listing_fee_paid";
}
