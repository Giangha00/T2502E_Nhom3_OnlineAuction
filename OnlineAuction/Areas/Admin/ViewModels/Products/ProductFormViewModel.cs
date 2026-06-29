using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Short description cannot exceed 300 characters.")]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [StringLength(160, ErrorMessage = "Subtitle cannot exceed 160 characters.")]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Display(Name = "Description")]
    public string? DescriptionHtml { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Seller is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a seller.")]
    [Display(Name = "Seller")]
    public int SellerId { get; set; }

    [Required(ErrorMessage = "Condition is required.")]
    [Display(Name = "Condition")]
    public string Condition { get; set; } = "New";

    [Display(Name = "Product Origin")]
    public string? ProductOrigin { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year between 1800 and 2100.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [StringLength(120)]
    [Display(Name = "Set Name")]
    public string? SetName { get; set; }

    [Display(Name = "Language")]
    public string? Language { get; set; } = "English";

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Grade Label")]
    public string? GradeLabel { get; set; } = "PSA 10";

    [Display(Name = "Certificate Number")]
    public string? CertNumber { get; set; }

    [Display(Name = "Grading — Centering")]
    public string? GradingCentering { get; set; } = "10";

    [Display(Name = "Grading — Corners")]
    public string? GradingCorners { get; set; } = "10";

    [Display(Name = "Grading — Edges")]
    public string? GradingEdges { get; set; } = "10";

    [Display(Name = "Grading — Surface")]
    public string? GradingSurface { get; set; } = "10";

    [Range(0, double.MaxValue, ErrorMessage = "Estimated value must be zero or greater.")]
    [Display(Name = "Estimated Value")]
    public decimal? EstimatedValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Import price must be zero or greater.")]
    [Display(Name = "Import Price")]
    public decimal? ImportPrice { get; set; }

    [Display(Name = "Primary Image")]
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

    public List<SelectListItem> CategoryOptions { get; set; } = [];

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
}
