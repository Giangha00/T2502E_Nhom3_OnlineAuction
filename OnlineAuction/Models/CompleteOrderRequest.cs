using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CompleteOrderRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, ErrorMessage = "Full name cannot exceed 120 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Shipping address is required.")]
    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a payment method.")]
    public string PaymentMethod { get; set; } = string.Empty;

    public List<int> SelectedOrderIds { get; set; } = [];
}
