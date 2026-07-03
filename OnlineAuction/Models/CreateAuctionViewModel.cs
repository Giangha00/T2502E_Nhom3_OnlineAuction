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

    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Registration Start")]
    [DataType(DataType.DateTime)]
    public DateTime RegistrationStartDate { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "Registration end is required.")]
    [Display(Name = "Registration End")]
    [DataType(DataType.DateTime)]
    public DateTime RegistrationEndDate { get; set; } = DateTime.Now.AddDays(7);

    [Required(ErrorMessage = "Live start is required.")]
    [Display(Name = "Live Start")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; } = DateTime.Now.AddDays(7);

    [Required(ErrorMessage = "Live end is required.")]
    [Display(Name = "Live End")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7).AddHours(1);
}
