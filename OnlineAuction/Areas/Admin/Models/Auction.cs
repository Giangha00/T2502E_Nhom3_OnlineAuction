using System.ComponentModel.DataAnnotations;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.Models;

public class Auction
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Starting price is required")]
    [Range(1, double.MaxValue, ErrorMessage = "Starting price must be greater than 0")]
    public decimal StartingPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CurrentPrice { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [Required]
    public AuctionType AuctionType { get; set; }

    [Required]
    public AuctionStatus Status { get; set; }

    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}