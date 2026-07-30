using OnlineAuction.Models;

namespace OnlineAuction.Entities;

public class Notification : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSON string[] of format args for <see cref="Message"/> when it is a resource key.
    /// </summary>
    public string? LocalizationArgsJson { get; set; }

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
    public const string SellerAwaitingPayment = "seller_awaiting_payment";
    public const string PaymentSuccess = "payment_success";
    public const string PaymentFailed = "payment_failed";
    public const string PaymentCancelled = "payment_cancelled";
    public const string SellerPaymentReceived = "seller_payment_received";
    public const string OrderCancelledPaymentOverdue = "order_cancelled_payment_overdue";
    public const string RefundApproved = "refund_approved";
    public const string RefundRequested = "refund_requested";
    public const string RefundRejected = "refund_rejected";
    public const string AuctionRegistrationConfirmed = "auction_registration_confirmed";
    public const string AuctionRegistrationCancelled = "auction_registration_cancelled";
    public const string AuctionDepositInitiated = "auction_deposit_initiated";
    public const string AuctionDepositCancelled = "auction_deposit_cancelled";
    public const string AuctionDepositFailed = "auction_deposit_failed";
    public const string AuctionDepositRefunded = "auction_deposit_refunded";
    public const string AuctionPaymentExpired = "auction_payment_expired";
    public const string AuctionSecondChanceOffered = "auction_second_chance_offered";
    public const string AuctionRelistRecommended = "auction_relist_recommended";
    public const string AuctionStartingSoon = "auction_starting_soon";
    public const string AuctionNowLive = "auction_now_live";
    public const string ListingFeePaid = "listing_fee_paid";
    public const string ListingSubmitted = "listing_submitted";
    public const string ListingUpdated = "listing_updated";
    public const string ListingCancelled = "listing_cancelled";
    public const string AuctionBidPlaced = "auction_bid_placed";
    public const string AuctionBidFailed = "auction_bid_failed";
    public const string AuctionNewBid = "auction_new_bid";
    public const string BuyNowOrderCreated = "buy_now_order_created";
    public const string RefundUnderReview = "refund_under_review";
    public const string RefundClosed = "refund_closed";
    public const string ProfileUpdated = "profile_updated";
    public const string ProfileUpdateFailed = "profile_update_failed";
}
