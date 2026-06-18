namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public class AuctionDetailViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }

    public decimal BidStep { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ListingType { get; set; } = string.Empty;

    public bool RequiresRegistration { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int BidCount { get; set; }

    public int RegistrationCount { get; set; }

    public string? WinnerName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
