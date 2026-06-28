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

    [Display(Name = "Condition")]
    public string Condition { get; set; } = "Graded";

    [StringLength(300, ErrorMessage = "Short description cannot exceed 300 characters.")]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [StringLength(160)]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Required(ErrorMessage = "Year is required.")]
    [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [Required(ErrorMessage = "Please select an authenticator.")]
    [Display(Name = "Authenticator")]
    public string Authenticator { get; set; } = "PSA";

    [Display(Name = "Grade")]
    public string GradeValue { get; set; } = "10";

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

    public List<string> Categories { get; set; } = [];
    public List<string> Authenticators { get; set; } = [];
    public List<string> GradeValues { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}
