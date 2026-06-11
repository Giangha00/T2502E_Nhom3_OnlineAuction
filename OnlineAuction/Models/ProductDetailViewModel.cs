namespace OnlineAuction.Models;

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal BidStep { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CountdownDays { get; set; }
    public int CountdownHours { get; set; }
    public int CountdownMinutes { get; set; }
    public string AuctionStatus { get; set; } = "Active Auction";
    public string StatusBadgeClass { get; set; } = "bg-emerald-600";
    public SellerViewModel Seller { get; set; } = new();
    public List<ProductDocumentViewModel> Documents { get; set; } = [];
    public List<AuctionItemViewModel> RelatedProducts { get; set; } = [];
}

public class ProductDocumentViewModel
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = "PDF";
}
