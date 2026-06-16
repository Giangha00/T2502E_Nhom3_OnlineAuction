using Microsoft.AspNetCore.Identity;
using OnlineAuction.Enums;

namespace OnlineAuction.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public ICollection<Product> Products { get; set; } = [];

    public ICollection<Auction> WonAuctions { get; set; } = [];

    public ICollection<Bid> Bids { get; set; } = [];

    public ICollection<AuctionOrder> Orders { get; set; } = [];
}
