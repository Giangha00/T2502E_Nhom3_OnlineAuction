using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockProductDetailData
{
    private static readonly Dictionary<string, string[]> CategoryExtraImages = new()
    {
        ["Pokémon"] =
        [
            "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=800&fit=crop"
        ],
        ["One Piece"] =
        [
            "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=800&fit=crop"
        ],
        ["Yu-Gi-Oh!"] =
        [
            "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1565538810643-b5bdb4dfa845?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=800&fit=crop"
        ],
        ["Sports"] =
        [
            "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1606107557195-0a29b4b9efab?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=800&h=800&fit=crop"
        ],
        ["Magic: The Gathering"] =
        [
            "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1606169046337-54513793d481?w=800&h=800&fit=crop",
            "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=800&fit=crop"
        ]
    };

    public static ProductDetailViewModel? GetById(int id)
    {
        var auction = MockAuctionData.GetAuctionById(id);
        if (auction is null) return null;

        var seller = MockAuctionData.GetSellerForAuction(id);
        if (seller is null) return null;
        var extras = CategoryExtraImages.GetValueOrDefault(auction.Category, CategoryExtraImages["Pokémon"]);
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
            Condition = string.IsNullOrEmpty(auction.Condition) ? "Graded" : auction.Condition,
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
            "Pokémon" => "Authenticated Pokémon card graded and vault-ready",
            "One Piece" => "Premium One Piece TCG card from a verified seller",
            "Yu-Gi-Oh!" => "Graded Yu-Gi-Oh! collectible for serious duelists",
            "Sports" => "Investment-grade sports card with documented provenance",
            "Magic: The Gathering" => "Rare MTG card authenticated for collectors",
            _ => "Curated trading card with verified seller history"
        };

    private static string BuildDescriptionHtml(AuctionItemViewModel auction) =>
        $"""
        <p>This <strong>{auction.Name}</strong> is offered through RareCard Vault with full seller disclosure and documented provenance. Ideal for collectors seeking a premium {auction.Category} piece.</p>
        <h3>Highlights</h3>
        <ul>
            <li>Category: {auction.Category}</li>
            <li>Grade: {auction.Grade}</li>
            <li>Set: {auction.Subtitle}</li>
            <li>Starting price: ${auction.StartingPrice:N0}</li>
            <li>Current highest bid: ${auction.CurrentPrice:N0}</li>
            <li>Authenticated listing reviewed by our curation team</li>
        </ul>
        <h3>Condition &amp; Notes</h3>
        <p>The card has been photographed from multiple angles inside its grading slab. Please review attached certificates and verification documents before bidding.</p>
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
