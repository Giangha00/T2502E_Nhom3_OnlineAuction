using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Ten mau is required.")]
    [StringLength(255, ErrorMessage = "Ten mau cannot exceed 255 characters.")]
    [Display(Name = "Ten mau")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Set Name")]
    public string? SetName { get; set; }

    [StringLength(50)]
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "Grade Label")]
    public string? GradeLabel { get; set; } = "PSA 10";

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year between 1800 and 2100.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [StringLength(50)]
    [Display(Name = "Language")]
    public string? Language { get; set; } = "English";

    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Description")]
    public string? DescriptionHtml { get; set; }

    [Display(Name = "Primary Image")]
    public IFormFile? PrimaryImageFile { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public bool HasInstances { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> GradeOptions { get; set; } = [];

    public List<SelectListItem> LanguageOptions { get; set; } = [];
}
