using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class SellProductFormViewModel
{
    [Required(ErrorMessage = "Card name is required.")]
    [StringLength(120, ErrorMessage = "Card name cannot exceed 120 characters.")]
    [Display(Name = "Card Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? ProductDescription { get; set; }

    [Required(ErrorMessage = "Please select a condition.")]
    [Display(Name = "Condition")]
    public string Condition { get; set; } = "New";

    [Display(Name = "Product Origin")]
    public string? ProductOrigin { get; set; }

    [StringLength(300, ErrorMessage = "Short description cannot exceed 300 characters.")]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [StringLength(160)]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [Display(Name = "Grade")]
    public string Grade { get; set; } = "PSA 10";

    [StringLength(120)]
    [Display(Name = "Set Name")]
    public string SetName { get; set; } = string.Empty;

    [Display(Name = "Language")]
    public string Language { get; set; } = "English";

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Certificate Number")]
    public string? CertificateNumber { get; set; }

    [Display(Name = "Primary Image")]
    public IFormFile? PrimaryImageFile { get; set; }

    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<IFormFile> DocumentFiles { get; set; } = [];

    public List<string> DocumentNames { get; set; } = [];

    [Display(Name = "Grading — Centering")]
    public string GradingCentering { get; set; } = "10";

    [Display(Name = "Grading — Corners")]
    public string GradingCorners { get; set; } = "10";

    [Display(Name = "Grading — Edges")]
    public string GradingEdges { get; set; } = "10";

    [Display(Name = "Grading — Surface")]
    public string GradingSurface { get; set; } = "10";

    public List<string> Categories { get; set; } = [];
    public List<string> Conditions { get; set; } = [];
    public List<string> Grades { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}
