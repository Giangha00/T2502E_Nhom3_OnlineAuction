namespace OnlineAuction.Entities;

public class Category : AuditableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];

    public ICollection<ProductTemplate> ProductTemplates { get; set; } = [];
}
