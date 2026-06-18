namespace OnlineAuction.Entities;

public class ProductImage : AuditableEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
