namespace OnlineAuction.Models;

using OnlineAuction.Entities;

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int Year { get; set; }
    public string SetName { get; set; } = string.Empty;
    public string Language { get; set; } = "English";
    public string CardNumber { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal BidStep { get; set; }
    public int BidCount { get; set; }
    public int LotNumber { get; set; }
    public int WatcherCount { get; set; }
    public bool ReserveMet { get; set; } = true;
    public string AuctionEventName { get; set; } = string.Empty;
    public List<decimal> QuickBidAmounts { get; set; } = [];
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegistrationStartDate { get; set; }
    public DateTime RegistrationEndDate { get; set; }
    public DateTime CountdownTargetDate { get; set; }
    public string ListingPhase { get; set; } = string.Empty;
    public string PhaseCountdownKind { get; set; } = string.Empty;
    public int CountdownDays { get; set; }
    public int CountdownHours { get; set; }
    public int CountdownMinutes { get; set; }
    public int CountdownSeconds { get; set; }
    public string AuctionStatus { get; set; } = "Active Auction";
    public string StatusBadgeClass { get; set; } = "bg-emerald-600";
    public bool CanPlaceBid { get; set; }
    public bool CanRegister { get; set; }
    public bool RequiresRegistration { get; set; } = true;
    public bool IsRegistered { get; set; }
    public string? RegistrationStatus { get; set; }
    public string? RegistrationRejectReason { get; set; }
    public bool CanBid { get; set; }
    public bool IsSeller { get; set; }
    public bool IsVerifiedAuthentic { get; set; }
    public int RegistrationCount { get; set; }
    public decimal RegistrationDepositAmount { get; set; }
    public SellerViewModel Seller { get; set; } = new();
    public GradingScoreViewModel Grading { get; set; } = new();
    public List<BidHistoryItemViewModel> BidHistory { get; set; } = [];
    public List<ProductDocumentViewModel> Documents { get; set; } = [];
    public List<AuctionItemViewModel> RelatedProducts { get; set; } = [];
    public List<AuctionItemViewModel> MoreRelatedProducts { get; set; } = [];
    public decimal? BuyNowPrice { get; set; }
    public string ListingType { get; set; } = ListingTypes.Auction;
    public string? ListingRejectReason { get; set; }
    public bool HasBuyNow =>
        string.Equals(ListingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase)
        || (BuyNowPrice.HasValue && BuyNowPrice.Value > 0);
    public bool CanPurchaseBuyNow =>
        !IsSeller && AuctionStatus is "Active Auction" or "Ending Soon";
}

public class ProductDocumentViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = "PDF";

    public string FileUrl { get; set; } = string.Empty;

    public bool ShowCertificateNumber { get; set; }
}
