namespace OnlineAuction.Data;

/// <summary>
/// Static option lists for sell forms (not product listings).
/// </summary>
public static class CreateAuctionMockData
{
    public static readonly IReadOnlyList<string> AuctionTypes =
    [
        "Normal",
        "Featured"
    ];

    public static readonly IReadOnlyList<string> Languages =
    [
        "English",
        "Japanese",
        "Korean",
        "Chinese",
        "Other"
    ];

    public static readonly IReadOnlyList<string> DocumentTypes =
    [
        "Certificate",
        "Warranty",
        "Invoice",
        "Verification"
    ];
}
