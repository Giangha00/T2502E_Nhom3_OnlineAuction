namespace OnlineAuction.Entities;

public class ProductTemplate : AuditableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public Category Category { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = [];
}
