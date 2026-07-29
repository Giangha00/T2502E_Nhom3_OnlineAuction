namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string? SetName { get; set; }

    public string? CardNumber { get; set; }

    public string? GradeLabel { get; set; }

    public string? Language { get; set; }

    public int? Year { get; set; }

    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    public string PrimaryImage { get; set; } = string.Empty;

    public int InstanceCount { get; set; }

    public int SellerCount { get; set; }
}
