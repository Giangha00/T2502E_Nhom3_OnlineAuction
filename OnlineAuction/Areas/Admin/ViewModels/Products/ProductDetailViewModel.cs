namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductDetailViewModel
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Subtitle { get; set; }

    public string? DescriptionHtml { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int? ProductTemplateId { get; set; }

    public string? ProductTemplateName { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string? SellerEmail { get; set; }

    public int SellerId { get; set; }

    public string Condition { get; set; } = string.Empty;

    public string? ProductOrigin { get; set; }

    public int? Year { get; set; }

    public string? SetName { get; set; }

    public string? Language { get; set; }

    public string? CardNumber { get; set; }

    public string? GradeLabel { get; set; }

    public string? CertNumber { get; set; }

    public string? GradingCentering { get; set; }

    public string? GradingCorners { get; set; }

    public string? GradingEdges { get; set; }

    public string? GradingSurface { get; set; }

    public decimal? EstimatedValue { get; set; }

    public decimal? ImportPrice { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public List<ProductImageItemViewModel> GalleryImages { get; set; } = [];

    public List<ProductDocumentItemViewModel> Documents { get; set; } = [];

    public List<ProductLinkedAuctionViewModel> LinkedAuctions { get; set; } = [];

    public bool CanDelete { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class ProductLinkedAuctionViewModel
{
    public int Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? PublicDetailUrl { get; set; }
}
