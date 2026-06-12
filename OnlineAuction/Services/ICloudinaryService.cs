namespace OnlineAuction.Services;

public interface ICloudinaryService
{
    Task<string?> UploadImageAsync(IFormFile file);
}