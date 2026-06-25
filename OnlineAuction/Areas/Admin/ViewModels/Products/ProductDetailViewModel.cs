namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Subtitle { get; set; }

    public string? DescriptionHtml { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string? ProductOrigin { get; set; }

    public int? Year { get; set; }

    public string? SetName { get; set; }

    public string? Language { get; set; }

    public string? CardNumber { get; set; }

    public string? GradeLabel { get; set; }

    public string? CertNumber { get; set; }

    public string? Centering { get; set; }

    public string? Corners { get; set; }

    public string? Edges { get; set; }

    public string? Surface { get; set; }

    public decimal? EstimatedValue { get; set; }

    public decimal? ImportPrice { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public List<ProductFormViewModel.GalleryImageItem> GalleryImages { get; set; } = [];

    public List<ProductFormViewModel.DocumentItem> Documents { get; set; } = [];

    public List<ProductAuctionItemViewModel> Auctions { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class ProductAuctionItemViewModel
{
    public int AuctionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
