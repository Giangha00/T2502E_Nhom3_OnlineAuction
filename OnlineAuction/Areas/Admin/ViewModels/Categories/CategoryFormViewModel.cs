using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Areas.Admin.ViewModels.Categories;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(60, ErrorMessage = "Slug cannot exceed 60 characters.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must use lowercase letters, numbers, and hyphens only.")]
    public string? Slug { get; set; }

    [Display(Name = "Sort Order")]
    [Range(0, 9999, ErrorMessage = "Sort order must be between 0 and 9999.")]
    public int SortOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public int ProductCount { get; set; }
}
