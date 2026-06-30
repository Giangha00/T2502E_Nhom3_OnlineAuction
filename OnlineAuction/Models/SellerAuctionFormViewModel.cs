using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class SellerAuctionFormViewModel
{
    public int? AuctionId { get; set; }

    [Required]
    [StringLength(120)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    public string? DescriptionHtml { get; set; }

    [Required]
    [StringLength(20)]
    public string Condition { get; set; } = "graded";

    public int? Year { get; set; }

    [StringLength(120)]
    public string? SetName { get; set; }

    [StringLength(20)]
    public string? GradeLabel { get; set; }

    [StringLength(50)]
    public string? CertNumber { get; set; }

    [Required]
    public string PrimaryImage { get; set; } = string.Empty;

    // File anh moi khi seller muon thay cover trong man hinh Edit.
    // Neu khong upload file moi thi Service giu nguyen PrimaryImage hien tai.
    public IFormFile? PrimaryImageFile { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal StartingPrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal BidStep { get; set; }

    [Required]
    public DateTime RegistrationStartDate { get; set; } = DateTime.UtcNow.AddHours(1);

    [Required]
    public DateTime RegistrationEndDate { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7).AddHours(1);
}
