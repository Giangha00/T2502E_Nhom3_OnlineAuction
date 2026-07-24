using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class UserProfileEditViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, ErrorMessage = "Full name cannot exceed 120 characters.")]
    [RegularExpression(@"^[a-zA-Z\sÀ-ỹ]+$", ErrorMessage = "Full name contains invalid characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(160, ErrorMessage = "Email cannot exceed 160 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\d{9,12}$", ErrorMessage = "Phone number must contain 9 to 12 digits.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Avatar")]
    public IFormFile? AvatarFile { get; set; }
}
