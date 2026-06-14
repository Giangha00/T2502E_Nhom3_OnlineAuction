namespace OnlineAuction.Services.Interfaces;

public interface IAvatarStorageService
{
    Task<string?> SaveAvatarAsync(IFormFile? avatarFile);
}