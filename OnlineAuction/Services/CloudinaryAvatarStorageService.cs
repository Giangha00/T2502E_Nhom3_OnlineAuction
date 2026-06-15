using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class CloudinaryAvatarStorageService : IAvatarStorageService
{
    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private const long MaxFileSize = 2 * 1024 * 1024;

    private readonly Cloudinary _cloudinary;

    public CloudinaryAvatarStorageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> SaveAvatarAsync(IFormFile? avatarFile)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return null;
        }

        if (avatarFile.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Avatar file size must not exceed 2MB.");
        }

        var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Avatar must be a JPG, PNG, or WEBP file.");
        }

        await using var stream = avatarFile.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(avatarFile.FileName, stream),
            Folder = "auction-house/users/avatars",
            Transformation = new Transformation()
                .Width(500)
                .Height(500)
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