using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CreateBuyNowViewModel : SellProductFormViewModel
{
    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    [Display(Name = "Price ($)")]
    public decimal Price { get; set; }
}
