namespace OnlineAuction.Helpers;

public static class UploadLimits
{
    /// <summary>Maximum image file size (1.5 MB).</summary>
    public const long MaxImageBytes = (long)(1.5 * 1024 * 1024);

    public const string MaxImageSizeLabel = "1.5MB";

    /// <summary>Gallery images in addition to the primary/cover image.</summary>
    public const int MaxGalleryImages = 4;

    /// <summary>Total images including primary/cover.</summary>
    public const int MaxTotalImages = MaxGalleryImages + 1;

    public const long MaxDocumentBytes = 5 * 1024 * 1024;
}
