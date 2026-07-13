using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "AdminProduct.Validation.ProductNameRequired")]
    [StringLength(120, ErrorMessage = "AdminProduct.Validation.ProductNameMaxLength")]
    [Display(Name = "AdminProduct.Field.ProductName")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "AdminProduct.Validation.ShortDescriptionMaxLength")]
    [Display(Name = "AdminProduct.Field.ShortDescription")]
    public string? ShortDescription { get; set; }

    [StringLength(160, ErrorMessage = "AdminProduct.Validation.SubtitleMaxLength")]
    [Display(Name = "AdminProduct.Field.Subtitle")]
    public string? Subtitle { get; set; }

    [Display(Name = "AdminProduct.Field.Description")]
    public string? DescriptionHtml { get; set; }

    [Display(Name = "AdminProduct.Field.Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "AdminProduct.Validation.TemplateRequired")]
    [Range(1, int.MaxValue, ErrorMessage = "AdminProduct.Validation.TemplateRequired")]
    [Display(Name = "AdminProduct.Field.Template")]
    public int? ProductTemplateId { get; set; }

    [Required(ErrorMessage = "AdminProduct.Validation.SellerRequired")]
    [Range(1, int.MaxValue, ErrorMessage = "AdminProduct.Validation.SellerRequired")]
    [Display(Name = "AdminProduct.Field.Seller")]
    public int SellerId { get; set; }

    [Required(ErrorMessage = "AdminProduct.Validation.ConditionRequired")]
    [Display(Name = "AdminProduct.Field.Condition")]
    public string Condition { get; set; } = "New";

    [Display(Name = "AdminProduct.Field.ProductOrigin")]
    public string? ProductOrigin { get; set; }

    [Range(1800, 2100, ErrorMessage = "AdminProduct.Validation.YearRange")]
    [Display(Name = "AdminProduct.Field.Year")]
    public int? Year { get; set; }

    [StringLength(120)]
    [Display(Name = "AdminProduct.Field.SetName")]
    public string? SetName { get; set; }

    [Display(Name = "AdminProduct.Field.Language")]
    public string? Language { get; set; } = "English";

    [Display(Name = "AdminProduct.Field.CardNumber")]
    public string? CardNumber { get; set; }

    [Display(Name = "AdminProduct.Field.GradeLabel")]
    public string? GradeLabel { get; set; } = "PSA 10";

    [Display(Name = "AdminProduct.Field.CertificateNumber")]
    public string? CertNumber { get; set; }

    [Display(Name = "AdminProduct.Field.GradingCentering")]
    public string? GradingCentering { get; set; } = "10";

    [Display(Name = "AdminProduct.Field.GradingCorners")]
    public string? GradingCorners { get; set; } = "10";

    [Display(Name = "AdminProduct.Field.GradingEdges")]
    public string? GradingEdges { get; set; } = "10";

    [Display(Name = "AdminProduct.Field.GradingSurface")]
    public string? GradingSurface { get; set; } = "10";

    [Range(0, double.MaxValue, ErrorMessage = "AdminProduct.Validation.EstimatedValueMin")]
    [Display(Name = "AdminProduct.Field.EstimatedValue")]
    public decimal? EstimatedValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AdminProduct.Validation.ImportPriceMin")]
    [Display(Name = "AdminProduct.Field.ImportPrice")]
    public decimal? ImportPrice { get; set; }

    [Display(Name = "AdminProduct.Field.PrimaryImage")]
    public IFormFile? PrimaryImageFile { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<IFormFile> DocumentFiles { get; set; } = [];

    public List<string> DocumentNames { get; set; } = [];

    public List<ProductImageItemViewModel> ExistingGalleryImages { get; set; } = [];

    public List<ProductDocumentItemViewModel> ExistingDocuments { get; set; } = [];

    public List<int> RemoveGalleryImageIds { get; set; } = [];

    public List<int> RemoveDocumentIds { get; set; } = [];

    public bool IsSellerLocked { get; set; }

    public bool IsTemplateLocked { get; set; }

    public string? ProductTemplateName { get; set; }

    public int? ContextTemplateId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> ProductTemplateOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> ConditionOptions { get; set; } = [];

    public List<SelectListItem> GradeOptions { get; set; } = [];

    public List<SelectListItem> LanguageOptions { get; set; } = [];
}

public class ProductImageItemViewModel
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class ProductDocumentItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = "PDF";

    public DateTime CreatedAt { get; set; }
}
