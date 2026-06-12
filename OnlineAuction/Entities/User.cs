using System.ComponentModel.DataAnnotations;
using OnlineAuction.Enums;

namespace OnlineAuction.Entities;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public Gender Gender { get; set; } = Gender.Other;

    [StringLength(260)]
    public string? AvatarUrl { get; set; }

    [StringLength(120)]
    public string InitialPassword { get; set; } = string.Empty;

    public int AuctionCount { get; set; }

    public bool HasActiveAuctionOrTransaction { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }
}
