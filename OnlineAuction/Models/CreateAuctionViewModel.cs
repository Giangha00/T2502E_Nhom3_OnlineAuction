using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CreateAuctionViewModel : SellProductFormViewModel
{
    public int? AuctionId { get; set; }

    public string? ExistingPrimaryImage { get; set; }

    [Required(ErrorMessage = "Starting price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0.")]
    [Display(Name = "Starting Price ($)")]
    public decimal StartingPrice { get; set; }

    [Required(ErrorMessage = "Bid step is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Bid step must be greater than 0.")]
    [Display(Name = "Bid Step ($)")]
    public decimal BidStep { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Buy now price must be greater than 0.")]
    [Display(Name = "Buy Now Price ($)")]
    public decimal? BuyNowPrice { get; set; }

    [Required(ErrorMessage = "Please select an auction type.")]
    [Display(Name = "Auction Type")]
    public string AuctionType { get; set; } = "Normal";

    [StringLength(160)]
    [Display(Name = "Auction Event")]
    public string AuctionEventName { get; set; } = "RareCard Vault: Premium Trading Card Auction 2026";

    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Start Date")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "End date is required.")]
    [Display(Name = "End Date")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);
}
