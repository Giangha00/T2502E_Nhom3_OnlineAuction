using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Areas.Admin.ViewModels.Products;

public class ProductTemplateFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "AdminProduct.Validation.TemplateNameRequired")]
    [StringLength(255, ErrorMessage = "AdminProduct.Validation.TemplateNameMaxLength")]
    [Display(Name = "AdminProduct.Field.TemplateName")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "AdminProduct.Validation.CategoryRequired")]
    [Range(1, int.MaxValue, ErrorMessage = "AdminProduct.Validation.CategoryRequired")]
    [Display(Name = "AdminProduct.Field.Category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "AdminProduct.Field.SetName")]
    public string? SetName { get; set; }

    [StringLength(50)]
    [Display(Name = "AdminProduct.Field.CardNumber")]
    public string? CardNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "AdminProduct.Field.GradeLabel")]
    public string? GradeLabel { get; set; } = "PSA 10";

    [Range(1800, 2100, ErrorMessage = "AdminProduct.Validation.YearRange")]
    [Display(Name = "AdminProduct.Field.Year")]
    public int? Year { get; set; }

    [StringLength(50)]
    [Display(Name = "AdminProduct.Field.Language")]
    public string? Language { get; set; } = "English";

    [Display(Name = "AdminProduct.Field.ShortDescription")]
    public string? ShortDescription { get; set; }

    [Display(Name = "AdminProduct.Field.Description")]
    public string? DescriptionHtml { get; set; }

    [Display(Name = "AdminProduct.Field.PrimaryImage")]
    public IFormFile? PrimaryImageFile { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public bool HasInstances { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> GradeOptions { get; set; } = [];

    public List<SelectListItem> LanguageOptions { get; set; } = [];
}
