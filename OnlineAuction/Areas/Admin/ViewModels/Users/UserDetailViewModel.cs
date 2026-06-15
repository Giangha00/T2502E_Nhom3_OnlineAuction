using OnlineAuction.Enums;

namespace OnlineAuction.Areas.Admin.ViewModels.Users;

public class UserDetailViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public int AuctionCount { get; set; }

    public bool HasActiveAuctionOrTransaction { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
