using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineAuction.Areas.Admin.ViewModels.Products;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;

namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int BidCount { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(300, ErrorMessage = "Short description cannot exceed 300 characters")]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Description")]
    public string? ProductDescription { get; set; }

    [StringLength(160)]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Display(Name = "Condition")]
    public string Condition { get; set; } = "Graded";

    [Required(ErrorMessage = "Year is required")]
    [Range(1800, 2100, ErrorMessage = "Please enter a valid year between 1800 and 2100")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [Required(ErrorMessage = "Please select an authenticator")]
    [Display(Name = "Authenticator")]
    public string Authenticator { get; set; } = "PSA";

    [Display(Name = "Grade")]
    public string GradeValue { get; set; } = "10";

    public string Grade { get; set; } = "PSA 10";

    [StringLength(120)]
    [Display(Name = "Set Name")]
    public string? SetName { get; set; }

    [Display(Name = "Language")]
    public string Language { get; set; } = "English";

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Certificate Number")]
    public string? CertificateNumber { get; set; }

    [Display(Name = "Primary Image")]
    public IFormFile? PrimaryImageFile { get; set; }

    public string? ImageUrl { get; set; }

    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<IFormFile> DocumentFiles { get; set; } = [];

    public List<string> DocumentNames { get; set; } = [];

    public List<ProductImageItemViewModel> ExistingGalleryImages { get; set; } = [];

    public List<ProductDocumentItemViewModel> ExistingDocuments { get; set; } = [];

    public List<int> RemoveGalleryImageIds { get; set; } = [];

    public List<int> RemoveDocumentIds { get; set; } = [];

    [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0")]
    [Display(Name = "Starting Price")]
    public decimal StartingPrice { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Bid step must be greater than 0")]
    [Display(Name = "Bid Step")]
    public decimal BidStep { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Buy now price must be greater than 0")]
    [Display(Name = "Buy Now Price")]
    public decimal? BuyNowPrice { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Display(Name = "Current Price")]
    public decimal CurrentPrice { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Registration Start")]
    public DateTime RegistrationStartDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Registration End")]
    public DateTime RegistrationEndDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Live Start")]
    public DateTime StartDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Live End")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [Display(Name = "Status")]
    public string Status { get; set; } = AuctionStatuses.Confirming;

    [Required(ErrorMessage = "Listing type is required")]
    [Display(Name = "Listing Type")]
    public string ListingType { get; set; } = ListingTypes.Auction;

    [Required(ErrorMessage = "Seller is required")]
    [Display(Name = "Seller")]
    public int SellerId { get; set; }

    public bool IsBuyNow => ListingType == ListingTypes.BuyNow;

    public bool IsEdit => Id > 0;

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> StatusOptions { get; set; } = [];

    public List<string> Authenticators { get; set; } = [];

    public List<string> GradeValues { get; set; } = [];

    public List<string> Languages { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Year.HasValue)
        {
            yield return new ValidationResult("Year is required.", [nameof(Year)]);
        }

        if (string.IsNullOrWhiteSpace(Authenticator))
        {
            yield return new ValidationResult("Please select an authenticator.", [nameof(Authenticator)]);
        }
        else if (!string.Equals(Authenticator, GradeLabelHelper.Ungraded, StringComparison.OrdinalIgnoreCase)
                 && string.IsNullOrWhiteSpace(GradeValue))
        {
            yield return new ValidationResult("Please select a grade.", [nameof(GradeValue)]);
        }

        if (Id == 0 && PrimaryImageFile is not { Length: > 0 } && string.IsNullOrWhiteSpace(ImageUrl))
        {
            yield return new ValidationResult("Primary image is required.", [nameof(PrimaryImageFile)]);
        }

        if (IsBuyNow)
        {
            if (Price <= 0)
            {
                yield return new ValidationResult("Price must be greater than 0.", [nameof(Price)]);
            }

            yield break;
        }

        if (StartingPrice <= 0)
        {
            yield return new ValidationResult("Starting price must be greater than 0.", [nameof(StartingPrice)]);
        }

        if (BidStep <= 0)
        {
            yield return new ValidationResult("Bid step must be greater than 0.", [nameof(BidStep)]);
        }

        if (BuyNowPrice.HasValue && BuyNowPrice.Value <= StartingPrice)
        {
            yield return new ValidationResult(
                "Buy now price must be greater than the starting price.",
                [nameof(BuyNowPrice)]);
        }

        var scheduleError = AuctionScheduleHelper.ValidateSchedule(
            RegistrationStartDate,
            RegistrationEndDate,
            StartDate,
            EndDate);

        if (scheduleError is not null)
        {
            yield return new ValidationResult(scheduleError, [nameof(RegistrationEndDate), nameof(StartDate)]);
        }

        if (Id == 0 && RegistrationStartDate < DateTime.Now.AddMinutes(-1))
        {
            yield return new ValidationResult(
                "Registration start cannot be in the past",
                [nameof(RegistrationStartDate)]);
        }

        if (BidCount > 0 && StartingPrice > CurrentPrice)
        {
            yield return new ValidationResult(
                "Starting price cannot exceed the current price when bids exist",
                [nameof(StartingPrice)]);
        }
    }

    public void NormalizeGrading()
    {
        Condition = GradeLabelHelper.ResolveCondition(Authenticator);
        Grade = GradeLabelHelper.Compose(Authenticator, GradeValue);
    }
}
