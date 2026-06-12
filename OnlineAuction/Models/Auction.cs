using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class Auction
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string Description { get; set; }

    public decimal StartPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public string ImageUrl { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = "Pending";

    public int CategoryId { get; set; }

    public Category Category { get; set; }
}