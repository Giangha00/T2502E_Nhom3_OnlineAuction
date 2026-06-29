using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Route("ProductDocument")]
public class ProductDocumentController : Controller
{
    private readonly IProductDocumentDownloadService _downloadService;

    public ProductDocumentController(IProductDocumentDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    [HttpGet("Download/{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var result = await _downloadService.GetDownloadAsync(id, isAdminRequest: false, cancellationToken);
        return ToDownloadActionResult(result);
    }

    private IActionResult ToDownloadActionResult(ProductDocumentDownloadInfo? result)
    {
        if (result is null)
        {
            return NotFound();
        }

        return result.Status switch
        {
            ProductDocumentDownloadStatus.NotFound => NotFound(),
            ProductDocumentDownloadStatus.Forbidden => Forbid(),
            ProductDocumentDownloadStatus.Success => Redirect(result.FileUrl),
            _ => NotFound()
        };
    }
}
