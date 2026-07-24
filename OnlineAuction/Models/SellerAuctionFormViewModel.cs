using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Models;

public class SellerAuctionFormViewModel : CreateAuctionViewModel
{
    public string Status { get; set; } = string.Empty;

    public bool HasBids { get; set; }

    /// <summary>
    /// confirming / rejected / scheduled before registration opens → full field edit.
    /// </summary>
    public bool CanEditFull { get; set; } = true;

    public bool LockRegistrationDates { get; set; }

    public bool LockLiveStartDate { get; set; }

    public bool LockStartingPrice { get; set; }

    public bool LockBidStep { get; set; }

    public List<SellerAuctionExistingImageViewModel> ExistingGalleryImages { get; set; } = [];

    public List<SellerAuctionExistingDocumentViewModel> ExistingDocuments { get; set; } = [];

    public List<int> RemovedGalleryImageIds { get; set; } = [];

    public List<int> RemovedDocumentIds { get; set; } = [];
}

public class SellerAuctionExistingImageViewModel
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class SellerAuctionExistingDocumentViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;
}
