namespace OnlineAuction.Entities;

public class ProductDocument : AuditableEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = "PDF";

    public Product Product { get; set; } = null!;
}
