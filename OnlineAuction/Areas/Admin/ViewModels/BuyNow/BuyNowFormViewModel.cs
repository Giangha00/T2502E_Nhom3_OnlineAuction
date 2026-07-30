using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Buy Now price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Buy Now price must be greater than 0")]
    [Display(Name = "Buy Now Price")]
    public decimal BuyNowPrice { get; set; }

    [Required(ErrorMessage = "Live start is required")]
    [Display(Name = "Live Start")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Live end is required")]
    [Display(Name = "Live End")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [Display(Name = "Status")]
    public string Status { get; set; } = AuctionStatuses.Live;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Seller is required")]
    [Display(Name = "Seller")]
    public int SellerId { get; set; }

    public string? ImageUrl { get; set; }

    [Display(Name = "Product Image")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Gallery Images")]
    public List<IFormFile> GalleryImageFiles { get; set; } = [];

    public List<BuyNowImageItemViewModel> ExistingGalleryImages { get; set; } = [];

    public List<int> RemoveGalleryImageIds { get; set; } = [];

    /// <summary>
    /// When true and no new primary ImageFile is uploaded, clear/replace primary image.
    /// </summary>
    public bool ClearPrimaryImage { get; set; }

    /// <summary>
    /// Existing gallery image id to promote as the new primary cover image.
    /// </summary>
    public int? PromoteGalleryImageId { get; set; }

    public bool HasOrders { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public List<SelectListItem> SellerOptions { get; set; } = [];

    public List<SelectListItem> StatusOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "Live end must be after live start.",
                [nameof(StartDate), nameof(EndDate)]);
        }
    }
}

public class BuyNowImageItemViewModel
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
