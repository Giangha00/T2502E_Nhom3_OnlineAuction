using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class ProductDocumentDownloadService : IProductDocumentDownloadService
{
    private readonly AuctionHouseDbContext _db;

    public ProductDocumentDownloadService(AuctionHouseDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDocumentDownloadInfo?> GetDownloadAsync(
        int documentId,
        bool isAdminRequest,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.ProductDocuments
            .AsNoTracking()
            .Include(d => d.Product)
                .ThenInclude(p => p.Auctions)
            .FirstOrDefaultAsync(
                d => d.Id == documentId && d.DeletedAt == null,
                cancellationToken);

        if (document is null || document.Product.DeletedAt != null)
        {
            return ProductDocumentDownloadInfo.NotFoundResult();
        }

        if (!isAdminRequest)
        {
            var auctionStatuses = document.Product.Auctions
                .Where(a => a.DeletedAt == null)
                .Select(a => a.Status)
                .ToList();

            if (!ProductDocumentAccessPolicy.CanAnonymousDownload(auctionStatuses))
            {
                return ProductDocumentDownloadInfo.ForbiddenResult();
            }
        }

        var fileName = ProductDocumentFileHelper.SanitizeDownloadFileName(document.Name);
        var downloadUrl = ProductDocumentFileHelper.BuildAttachmentUrl(document.FileUrl);

        return ProductDocumentDownloadInfo.SuccessResult(downloadUrl, fileName);
    }
}

public static class ProductDocumentFileHelper
{
    public static string SanitizeDownloadFileName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "document.pdf";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(trimmed.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "document.pdf";
        }

        return cleaned.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? cleaned
            : $"{cleaned}.pdf";
    }

    public static string BuildAttachmentUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return fileUrl;
        }

        if (!fileUrl.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return fileUrl;
        }

        const string uploadSegment = "/upload/";
        var index = fileUrl.IndexOf(uploadSegment, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return fileUrl;
        }

        var insertAt = index + uploadSegment.Length;
        if (fileUrl.AsSpan(insertAt).StartsWith("fl_attachment/", StringComparison.OrdinalIgnoreCase))
        {
            return fileUrl;
        }

        return fileUrl.Insert(insertAt, "fl_attachment/");
    }
}
