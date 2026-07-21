namespace OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;

public class AuctionVerificationDetailViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string? GradeLabel { get; set; }

    public string? CertNumber { get; set; }

    public string? Language { get; set; }

    public string? CardNumber { get; set; }

    public int? Year { get; set; }

    public string? SetName { get; set; }

    public string? GradingCentering { get; set; }

    public string? GradingCorners { get; set; }

    public string? GradingEdges { get; set; }

    public string? GradingSurface { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public IReadOnlyList<string> GalleryImages { get; set; } = [];

    public IReadOnlyList<VerificationDocumentViewModel> Documents { get; set; } = [];

    public decimal StartingPrice { get; set; }

    public decimal BidStep { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public DateTime RegistrationStartDate { get; set; }

    public DateTime RegistrationEndDate { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? AuctionEventName { get; set; }

    public bool RequiresRegistration { get; set; }

    public string ListingType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string AuctionPublicVisibility { get; set; } = "No";

    public string AuctionPublicVisibilityReason { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public int SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public bool IsBuyNow =>
        string.Equals(ListingType, Entities.ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase);

    public bool IsAuction => !IsBuyNow;

    public bool HasSubGrades =>
        !string.IsNullOrWhiteSpace(GradingCentering)
        || !string.IsNullOrWhiteSpace(GradingCorners)
        || !string.IsNullOrWhiteSpace(GradingEdges)
        || !string.IsNullOrWhiteSpace(GradingSurface);

    public bool HasAuctionEvent => !string.IsNullOrWhiteSpace(AuctionEventName);

    public decimal DisplayPrice =>
        IsBuyNow
            ? (BuyNowPrice ?? StartingPrice)
            : StartingPrice;

    public bool HasRealPrimaryImage =>
        !string.IsNullOrWhiteSpace(PrimaryImage)
        && !PrimaryImage.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
        && !PrimaryImage.Contains("via.placeholder", StringComparison.OrdinalIgnoreCase);

    public bool HasDescription =>
        !string.IsNullOrWhiteSpace(ShortDescription)
        || !string.IsNullOrWhiteSpace(DescriptionHtml);

    public bool HasDocuments => Documents.Count > 0;

    public bool HasValidPricing =>
        IsBuyNow
            ? DisplayPrice > 0
            : StartingPrice > 0 && BidStep > 0;

    public bool HasValidSchedule =>
        IsBuyNow
            || (RegistrationStartDate < RegistrationEndDate
                && RegistrationEndDate <= StartDate
                && StartDate < EndDate);
}

public class VerificationDocumentViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
