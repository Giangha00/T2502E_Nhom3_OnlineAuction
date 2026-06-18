using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Starting price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0")]
    [Display(Name = "Starting Price")]
    public decimal StartingPrice { get; set; }

    [Required(ErrorMessage = "Bid step is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Bid step must be greater than 0")]
    [Display(Name = "Bid Step")]
    public decimal BidStep { get; set; }

    [Display(Name = "Current Price")]
    public decimal CurrentPrice { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [Display(Name = "Status")]
    public string Status { get; set; } = AuctionStatuses.Live;

    [Required(ErrorMessage = "Listing type is required")]
    [Display(Name = "Listing Type")]
    public string ListingType { get; set; } = ListingTypes.Auction;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Seller is required")]
    [Display(Name = "Seller")]
    public int SellerId { get; set; }

    [Display(Name = "Requires Registration")]
    public bool RequiresRegistration { get; set; } = true;

    public string? ImageUrl { get; set; }

    [Display(Name = "Product Image")]
    public IFormFile? ImageFile { get; set; }

    public int BidCount { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> StatusOptions { get; set; } = [];

    public List<SelectListItem> ListingTypeOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "End date must be greater than start date",
                [nameof(EndDate)]);
        }

        if (Id == 0 && StartDate < DateTime.Now.AddMinutes(-1))
        {
            yield return new ValidationResult(
                "Start date cannot be in the past",
                [nameof(StartDate)]);
        }

        if (BidCount > 0 && StartingPrice > CurrentPrice)
        {
            yield return new ValidationResult(
                "Starting price cannot exceed the current price when bids exist",
                [nameof(StartingPrice)]);
        }
    }
}
