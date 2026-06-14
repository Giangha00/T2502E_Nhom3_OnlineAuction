using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CreateAuctionViewModel
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters.")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Product Description")]
    public string? ProductDescription { get; set; }

    [Required(ErrorMessage = "Please select a condition.")]
    [Display(Name = "Condition")]
    public string Condition { get; set; } = "New";

    [Display(Name = "Product Origin")]
    public string? ProductOrigin { get; set; }

    [Required(ErrorMessage = "Starting price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0.")]
    [Display(Name = "Starting Price ($)")]
    public decimal StartingPrice { get; set; }

    [Required(ErrorMessage = "Bid step is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Bid step must be greater than 0.")]
    [Display(Name = "Bid Step ($)")]
    public decimal BidStep { get; set; }

    [Display(Name = "Buy Now Price ($)")]
    public decimal? BuyNowPrice { get; set; }

    [Required(ErrorMessage = "Please select an auction type.")]
    [Display(Name = "Auction Type")]
    public string AuctionType { get; set; } = "Normal";

    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Start Date")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "End date is required.")]
    [Display(Name = "End Date")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

    public List<string> Categories { get; set; } = [];
    public List<string> Conditions { get; set; } = [];
}
