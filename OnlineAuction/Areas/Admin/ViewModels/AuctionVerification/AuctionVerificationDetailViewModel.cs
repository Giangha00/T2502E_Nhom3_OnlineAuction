namespace OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;

public class AuctionVerificationDetailViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

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

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? AuctionEventName { get; set; }

    public bool RequiresRegistration { get; set; }

    public string ListingType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public int SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;
}

public class VerificationDocumentViewModel
{
    public string Name { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;
}
