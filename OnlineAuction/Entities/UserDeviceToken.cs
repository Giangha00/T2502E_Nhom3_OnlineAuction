namespace OnlineAuction.Entities;

public class UserDeviceToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FcmToken { get; set; } = string.Empty;

    public string? DeviceInfo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
