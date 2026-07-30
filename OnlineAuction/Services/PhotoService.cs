using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PhotoService : IPhotoService
{
    private static readonly string[] AllowedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private static readonly string[] AllowedDocumentExtensions =
    [
        ".pdf"
    ];

    private const long MaxImageBytes = UploadLimits.MaxImageBytes;
    private const long MaxDocumentBytes = UploadLimits.MaxDocumentBytes;

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

        var isDocumentFolder = folder.Contains("documents", StringComparison.OrdinalIgnoreCase);
        var maxBytes = isDocumentFolder ? MaxDocumentBytes : MaxImageBytes;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException(isDocumentFolder
                ? "Document file size must not exceed 5MB."
                : $"Image file size must not exceed {UploadLimits.MaxImageSizeLabel}.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (isDocumentFolder)
        {
            if (!AllowedDocumentExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Document must be a PDF file.");
            }

            return await UploadRawAsync(file, folder);
        }

        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Image must be a JPG, PNG, or WEBP file.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            // Keep the full uploaded image (no edge crop). Preview uses the original
            // blob, so listing delivery must not use fill/crop either.
            Transformation = new Transformation()
                .Width(1200)
                .Height(1500)
                .Crop("limit")
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

    private async Task<string> UploadRawAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.AbsoluteUri;
    }
}
