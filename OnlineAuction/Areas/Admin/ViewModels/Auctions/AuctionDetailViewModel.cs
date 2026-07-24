namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionDetailViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public decimal BidStep { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ListingPhase { get; set; } = string.Empty;

    public string ListingPhaseLabel { get; set; } = string.Empty;

    public bool IsPubliclyListed { get; set; }

    public DateTime? CountdownTargetDate { get; set; }

    public string PhaseCountdownKind { get; set; } = string.Empty;

    public string TimeRemaining { get; set; } = string.Empty;

    public string ListingType { get; set; } = string.Empty;

    public bool RequiresRegistration { get; set; }

    public DateTime RegistrationStartDate { get; set; }

    public DateTime RegistrationEndDate { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int BidCount { get; set; }

    public int RegistrationCount { get; set; }

    public string? WinnerName { get; set; }

    public int? OrderId { get; set; }

    public string? OrderReference { get; set; }

    public string? OrderStatus { get; set; }

    public DateTime? PaymentDeadline { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? VerifiedByName { get; set; }

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<AdminBidHistoryItemViewModel> BidHistory { get; set; } = [];

    public IReadOnlyList<FraudAlertViewModel> FraudAlerts { get; set; } = [];

    public bool ShowFlaggedBidsOnly { get; set; }

    public int BidHistoryTotalCount { get; set; }

    public int BidHistoryPage { get; set; } = 1;

    public int BidHistoryPageSize { get; set; } = 20;

    public int BidHistoryTotalPages { get; set; } = 1;

    public IReadOnlyList<AdminWinnerNonPaymentLogViewModel> WinnerNonPaymentLogs { get; set; } = [];

    public IReadOnlyList<AdminForfeitedDepositViewModel> ForfeitedDeposits { get; set; } = [];

    public bool CanDelete { get; set; } = true;

    public bool CanCancel { get; set; }

    public string? DeleteBlockReason { get; set; }

    public int OrderItemCount { get; set; }
}
