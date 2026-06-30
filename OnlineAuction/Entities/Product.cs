namespace OnlineAuction.Entities;

public class Product : AuditableEntity
{
    public int Id { get; set; }

    public int SellerId { get; set; }

    public int CategoryId { get; set; }

    public int? ProductTemplateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Subtitle { get; set; }

    public string? DescriptionHtml { get; set; }

    public string Condition { get; set; } = "graded";

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

    public string PrimaryImage { get; set; } = string.Empty;

    public decimal? EstimatedValue { get; set; }

    public decimal? ImportPrice { get; set; }

    public ApplicationUser Seller { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public ProductTemplate? ProductTemplate { get; set; }

    public ICollection<Auction> Auctions { get; set; } = [];

    public ICollection<ProductImage> Images { get; set; } = [];

    public ICollection<ProductDocument> Documents { get; set; } = [];
}
