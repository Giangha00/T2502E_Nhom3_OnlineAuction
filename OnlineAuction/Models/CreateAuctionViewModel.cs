using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class CreateAuctionViewModel
{
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
    public string Condition { get; set; } = "Graded";

    [Required(ErrorMessage = "Please select a grade.")]
    [Display(Name = "Grade")]
    public string Grade { get; set; } = "PSA 10";

    [Display(Name = "Subtitle")]
    public string? Subtitle { get; set; }

    [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")]
    [Display(Name = "Year")]
    public int? Year { get; set; }

    [Required(ErrorMessage = "Set name is required.")]
    [StringLength(120)]
    [Display(Name = "Set Name")]
    public string SetName { get; set; } = string.Empty;

    [Display(Name = "Language")]
    public string Language { get; set; } = "English";

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Certificate Number")]
    public string? CertificateNumber { get; set; }

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

    [Required(ErrorMessage = "Estimated value is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Estimated value must be greater than 0.")]
    [Display(Name = "Estimated Value ($)")]
    public decimal EstimatedValue { get; set; }

    [Required(ErrorMessage = "Auction event name is required.")]
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

    public List<string> Categories { get; set; } = [];
    public List<string> Conditions { get; set; } = [];
    public List<string> Grades { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}
