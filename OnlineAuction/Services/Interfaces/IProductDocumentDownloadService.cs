namespace OnlineAuction.Services.Interfaces;

public interface IProductDocumentDownloadService
{
    Task<ProductDocumentDownloadInfo?> GetDownloadAsync(
        int documentId,
        bool isAdminRequest,
        CancellationToken cancellationToken = default);
}

public enum ProductDocumentDownloadStatus
{
    Success,
    NotFound,
    Forbidden
}

public sealed class ProductDocumentDownloadInfo
{
    public ProductDocumentDownloadStatus Status { get; init; }

    public string FileUrl { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public static ProductDocumentDownloadInfo NotFoundResult() =>
        new() { Status = ProductDocumentDownloadStatus.NotFound };

    public static ProductDocumentDownloadInfo ForbiddenResult() =>
        new() { Status = ProductDocumentDownloadStatus.Forbidden };

    public static ProductDocumentDownloadInfo SuccessResult(string fileUrl, string fileName) =>
        new()
        {
            Status = ProductDocumentDownloadStatus.Success,
            FileUrl = fileUrl,
            FileName = fileName
        };
}
