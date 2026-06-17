using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CreateAuctionViewModel
{
    // Dung cho man hinh Edit. Khi tao moi thi gia tri nay de trong.
    public int? AuctionId { get; set; }

    [Required(ErrorMessage = "Card name is required.")]
    [StringLength(120, ErrorMessage = "Card name cannot exceed 120 characters.")]
    [Display(Name = "Card Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? ProductDescription { get; set; }

    [Required(ErrorMessage = "Please select a condition.")]
    [Display(Name = "Condition")]
    public string Condition { get; set; } = "New";

    [Display(Name = "Product Origin")]
    public string? ProductOrigin { get; set; }

    [StringLength(300, ErrorMessage = "Short description cannot exceed 300 characters.")]
    [Display(Name = "Short Description")]
    public string? ShortDescription { get; set; }

    [StringLength(160)]
    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [Display(Name = "Grade")]
    public string Grade { get; set; } = "PSA 10";

    [StringLength(120)]
    [Display(Name = "Set Name")]
    public string SetName { get; set; } = string.Empty;

    [Display(Name = "Language")]
    public string Language { get; set; } = "English";

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Certificate Number")]
    public string? CertificateNumber { get; set; }

    // File anh chinh lay tu form va upload len Cloudinary trong Service.
    // Database khong luu file, chi luu URL Cloudinary vao cot products.primary_image.
    [Display(Name = "Primary Image")]
    public IFormFile? PrimaryImageFile { get; set; }

    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<IFormFile> DocumentFiles { get; set; } = [];

    public List<string> DocumentNames { get; set; } = [];

    // Dung khi sua auction: neu seller khong chon anh moi thi giu URL anh cu.
    public string? ExistingPrimaryImage { get; set; }

    [Display(Name = "Grading — Centering")]
    public string GradingCentering { get; set; } = "10";

    [Display(Name = "Grading — Corners")]
    public string GradingCorners { get; set; } = "10";

    [Display(Name = "Grading — Edges")]
    public string GradingEdges { get; set; } = "10";

    [Display(Name = "Grading — Surface")]
    public string GradingSurface { get; set; } = "10";

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

    [Range(0.01, double.MaxValue, ErrorMessage = "Estimated value must be greater than 0.")]
    [Display(Name = "Estimated Value ($)")]
    public decimal? EstimatedValue { get; set; }

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
    public List<string> Grades { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}
