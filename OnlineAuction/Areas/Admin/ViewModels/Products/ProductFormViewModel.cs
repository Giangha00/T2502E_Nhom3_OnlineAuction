using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductFormViewModel : IValidatableObject
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
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Seller is required.")]
    [Display(Name = "Seller")]
    public int SellerId { get; set; }

    [Required(ErrorMessage = "Condition is required.")]
    [StringLength(20, ErrorMessage = "Condition cannot exceed 20 characters.")]
    [Display(Name = "Condition")]
    public string Condition { get; set; } = "graded";

    [StringLength(120, ErrorMessage = "Product origin cannot exceed 120 characters.")]
    [Display(Name = "Product Origin")]
    public string? ProductOrigin { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year between 1800 and 2100.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [StringLength(120, ErrorMessage = "Set name cannot exceed 120 characters.")]
    [Display(Name = "Set Name")]
    public string? SetName { get; set; }

    [StringLength(20, ErrorMessage = "Language cannot exceed 20 characters.")]
    [Display(Name = "Language")]
    public string? Language { get; set; }

    [StringLength(30, ErrorMessage = "Card number cannot exceed 30 characters.")]
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [StringLength(20, ErrorMessage = "Grade label cannot exceed 20 characters.")]
    [Display(Name = "Grade Label")]
    public string? GradeLabel { get; set; }

    [StringLength(50, ErrorMessage = "Certificate number cannot exceed 50 characters.")]
    [Display(Name = "Cert Number")]
    public string? CertNumber { get; set; }

    [StringLength(10, ErrorMessage = "Centering cannot exceed 10 characters.")]
    [Display(Name = "Centering")]
    public string? Centering { get; set; }

    [StringLength(10, ErrorMessage = "Corners cannot exceed 10 characters.")]
    [Display(Name = "Corners")]
    public string? Corners { get; set; }

    [StringLength(10, ErrorMessage = "Edges cannot exceed 10 characters.")]
    [Display(Name = "Edges")]
    public string? Edges { get; set; }

    [StringLength(10, ErrorMessage = "Surface cannot exceed 10 characters.")]
    [Display(Name = "Surface")]
    public string? Surface { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Estimated value must be zero or greater.")]
    [Display(Name = "Estimated Value")]
    public decimal? EstimatedValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Import price must be zero or greater.")]
    [Display(Name = "Import Price")]
    public decimal? ImportPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater.")]
    [Display(Name = "Price")]
    public decimal? Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater.")]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; } = 1;

    public string? PrimaryImageUrl { get; set; }

    [Display(Name = "Primary Image")]
    public IFormFile? PrimaryImageFile { get; set; }

    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<IFormFile> DocumentFiles { get; set; } = [];

    public List<string> DocumentNames { get; set; } = [];

    public List<GalleryImageItem> ExistingGalleryImages { get; set; } = [];

    public List<DocumentItem> ExistingDocuments { get; set; } = [];

    public List<int> RemovedGalleryImageIds { get; set; } = [];

    public List<int> RemovedDocumentIds { get; set; } = [];

    public bool CanChangeSeller { get; set; } = true;

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> ConditionOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Id.HasValue && (PrimaryImageFile is null || PrimaryImageFile.Length == 0))
        {
            yield return new ValidationResult(
                "Primary image is required.",
                [nameof(PrimaryImageFile)]);
        }

        var galleryCount = GalleryImageFiles.Count(file => file is { Length: > 0 });
        var existingGalleryCount = ExistingGalleryImages.Count(image => !RemovedGalleryImageIds.Contains(image.Id));
        if (1 + existingGalleryCount + galleryCount > 5)
        {
            yield return new ValidationResult(
                "You can upload up to 5 images in total (including the primary image).",
                [nameof(GalleryImageFiles)]);
        }
    }

    public class GalleryImageItem
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }

    public class DocumentItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;
    }
}
