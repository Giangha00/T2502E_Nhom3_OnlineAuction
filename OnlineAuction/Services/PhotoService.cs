using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PhotoService : IPhotoService
{
    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly Cloudinary _cloudinary;

    public PhotoService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;

        // CloudinarySettings duoc map tu appsettings.json trong Program.cs.
        // Khong de Controller biet Cloudinary hoat dong the nao.
        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> AddPhotoAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Image file size must not exceed 5MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Image must be a JPG, PNG, or WEBP file.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            // Cloudinary se resize/crop anh de anh listing nhe va deu khung.
            Transformation = new Transformation()
                .Width(900)
                .Height(900)
                .Crop("fill")
                .Quality("auto")
                .FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.AbsoluteUri;
    }
}
