namespace OnlineAuction.Entities;

public class ProductTemplate : AuditableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string? SetName { get; set; }

    public string? CardNumber { get; set; }

    public string? GradeLabel { get; set; }

    public int? Year { get; set; }

    public string? Language { get; set; }

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Category Category { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = [];
}
