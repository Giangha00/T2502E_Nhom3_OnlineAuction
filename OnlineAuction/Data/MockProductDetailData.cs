using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockProductDetailData
{
    private static readonly Dictionary<string, string[]> CategoryExtraImages = new()
    {
        ["Cars"] =
        [
            "https://images.unsplash.com/photo-1583121274602-3e2820c50fa8?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1502877338535-766e1452684a?w=800&h=800&fit=crop"
        ],
        ["Watches"] =
        [
            "https://images.unsplash.com/photo-1548171916-e79a860ad597?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1614164185128-e4ec99c436d7?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1587836374828-4dbafa94cf0e?w=800&h=800&fit=crop"
        ],
        ["Cards"] =
        [
            "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=800&fit=crop"
        ],
        ["Billiard Sticks"] =
        [
            "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1571019614242-c5c25dee48f8?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1609710228159-0fa9bd7c0827?w=800&h=800&fit=crop"
        ],
        ["Jewelry"] =
        [
            "https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1599643478518-a784e5dc4c2f?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=800&h=800&fit=crop"
        ]
    };

    public static ProductDetailViewModel? GetById(int id)
    {
        var auction = MockAuctionData.GetAuctionById(id);
        if (auction is null) return null;

        var seller = MockAuctionData.GetSellerForAuction(id);
        if (seller is null) return null;
        var extras = CategoryExtraImages.GetValueOrDefault(auction.Category, CategoryExtraImages["Watches"]);
        var images = new List<string> { auction.ImageUrl };
        images.AddRange(extras.Take(3));

        var (days, hours, minutes) = ParseCountdown(auction.TimeRemaining);
        var (status, badgeClass) = MapStatus(auction.Status);
        var bidStep = CalculateBidStep(auction.StartingPrice);
        var (start, end) = BuildAuctionDates(id, days, hours, minutes);

        return new ProductDetailViewModel
        {
            Id = auction.Id,
            Name = auction.Name,
            ShortDescription = BuildShortDescription(auction),
            Category = auction.Category,
            Condition = (id % 3) switch { 0 => "Like New", 1 => "Used", _ => "New" },
            DescriptionHtml = BuildDescriptionHtml(auction),
            Images = images,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.CurrentPrice,
            BidStep = bidStep,
            StartDate = start,
            EndDate = end,
            CountdownDays = days,
            CountdownHours = hours,
            CountdownMinutes = minutes,
            AuctionStatus = status,
            StatusBadgeClass = badgeClass,
            Seller = seller,
            Documents = BuildDocuments(auction),
            RelatedProducts = MockAuctionData.GetAllAuctions()
                .Where(a => a.Category == auction.Category && a.Id != auction.Id)
                .Take(4)
                .ToList()
        };
    }

    private static string BuildShortDescription(AuctionItemViewModel auction) =>
        auction.Category switch
        {
            "Cars" => "Classic automobile in excellent collector condition",
            "Watches" => "Premium timepiece from a trusted collector",
            "Cards" => "Authenticated trading card for serious collectors",
            "Billiard Sticks" => "Professional-grade cue with verified provenance",
            "Jewelry" => "Fine jewelry piece with documented authenticity",
            _ => "Curated auction item with verified seller history"
        };

    private static string BuildDescriptionHtml(AuctionItemViewModel auction) =>
        $"""
        <p>This <strong>{auction.Name}</strong> is offered through Auction House with full seller disclosure and documented provenance. Ideal for collectors seeking a premium {auction.Category.ToLowerInvariant()} piece.</p>
        <h3>Highlights</h3>
        <ul>
            <li>Category: {auction.Category}</li>
            <li>Starting price: ${auction.StartingPrice:N0}</li>
            <li>Current highest bid: ${auction.CurrentPrice:N0}</li>
            <li>Authenticated listing reviewed by our curation team</li>
        </ul>
        <h3>Condition &amp; Notes</h3>
        <p>The item has been photographed from multiple angles. Minor wear may be visible in close-up images. Please review attached certificates and verification documents before bidding.</p>
        <p><a href="#">View seller return policy</a> · <a href="#">Ask a question</a></p>
        """;

    private static List<ProductDocumentViewModel> BuildDocuments(AuctionItemViewModel auction) =>
    [
        new() { Name = "Product Certificate", FileName = $"{Slugify(auction.Name)}_Certificate.pdf", FileType = "PDF" },
        new() { Name = "Warranty", FileName = $"{Slugify(auction.Name)}_Warranty.pdf", FileType = "PDF" },
        new() { Name = "Product Verification", FileName = $"{Slugify(auction.Name)}_Verification.pdf", FileType = "PDF" }
    ];

    private static decimal CalculateBidStep(decimal startingPrice) =>
        startingPrice switch
        {
            >= 20000 => 500,
            >= 5000 => 100,
            >= 1000 => 50,
            _ => 10
        };

    private static (DateTime Start, DateTime End) BuildAuctionDates(int id, int days, int hours, int minutes)
    {
        var end = DateTime.Now.AddDays(days).AddHours(hours).AddMinutes(minutes);
        var start = end.AddDays(-7 - (id % 5));
        return (start, end);
    }

    private static (int Days, int Hours, int Minutes) ParseCountdown(string timeRemaining)
    {
        var days = 0;
        var hours = 0;
        var minutes = 0;

        var dayMatch = System.Text.RegularExpressions.Regex.Match(timeRemaining, @"(\d+)\s*d");
        if (dayMatch.Success) days = int.Parse(dayMatch.Groups[1].Value);

        var hourMatch = System.Text.RegularExpressions.Regex.Match(timeRemaining, @"(\d+)\s*h");
        if (hourMatch.Success) hours = int.Parse(hourMatch.Groups[1].Value);

        var minuteMatch = System.Text.RegularExpressions.Regex.Match(timeRemaining, @"(\d+)\s*m");
        if (minuteMatch.Success) minutes = int.Parse(minuteMatch.Groups[1].Value);

        if (days == 0 && hours == 0 && minutes == 0) minutes = 30;
        return (days, hours, minutes);
    }

    private static (string Status, string BadgeClass) MapStatus(string status) => status switch
    {
        "Ending Soon" => ("Ending Soon", "bg-orange-600"),
        "Won" or "Completed" => ("Completed", "bg-stone-600"),
        _ => ("Active Auction", "bg-emerald-600")
    };

    private static string Slugify(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit).Take(24));
}
