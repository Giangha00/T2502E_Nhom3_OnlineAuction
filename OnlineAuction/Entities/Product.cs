namespace OnlineAuction.Entities;

public class Product : AuditableEntity
{
    public int Id { get; set; }

    public int SellerId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string Condition { get; set; } = "graded";

    public int? Year { get; set; }

    public string? SetName { get; set; }

    public string? GradeLabel { get; set; }

    public string? CertNumber { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public decimal? ImportPrice { get; set; }

    public ApplicationUser Seller { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public ICollection<Auction> Auctions { get; set; } = [];
}
