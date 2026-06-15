namespace OnlineAuction.Entities;

public class Product
{
    public int Id { get; set; }

    public int SellerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string Condition { get; set; } = "graded";

    public int? Year { get; set; }

    public string? SetName { get; set; }

    public string? GradeLabel { get; set; }

    public string? CertNumber { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<Auction> Auctions { get; set; } = [];
}
