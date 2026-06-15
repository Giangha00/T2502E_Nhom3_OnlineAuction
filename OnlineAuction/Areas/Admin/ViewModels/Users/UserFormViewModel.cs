using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineAuction.Enums;

namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, ErrorMessage = "Full name cannot exceed 120 characters.")]
    [RegularExpression(@"^[a-zA-Z\sÀ-ỹ]+$", ErrorMessage = "Full name contains invalid characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username contains invalid characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(160, ErrorMessage = "Email cannot exceed 160 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\d{9,12}$", ErrorMessage = "Phone number must contain 9 to 12 digits.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public UserRole Role { get; set; } = UserRole.User;

    [Required(ErrorMessage = "Status is required.")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    [StringLength(120, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 120 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? InitialPassword { get; set; }

    public string? CurrentAvatarUrl { get; set; }

    [Display(Name = "Avatar")]
    public IFormFile? AvatarFile { get; set; }

    public List<SelectListItem> RoleOptions { get; set; } = [];

    public List<SelectListItem> StatusOptions { get; set; } = [];
}
