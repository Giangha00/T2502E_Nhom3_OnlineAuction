using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class VerifyPasswordOtpViewModel
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Enter the 6-digit verification code.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit verification code.")]
    [Display(Name = "Verification code")]
    public string Otp { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
