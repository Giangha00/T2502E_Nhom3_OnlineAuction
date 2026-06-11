namespace OnlineAuction.Data;

public static class CreateAuctionMockData
{
    public static readonly IReadOnlyList<string> Categories =
    [
        "Electronics",
        "Fashion",
        "Collectibles",
        "Artwork"
    ];

    public static readonly IReadOnlyList<string> Conditions =
    [
        "New",
        "Like New",
        "Used"
    ];

    public static readonly IReadOnlyList<string> AuctionTypes =
    [
        "Normal",
        "Featured"
    ];

    public static readonly IReadOnlyList<string> DocumentTypes =
    [
        "Certificate",
        "Warranty",
        "Invoice",
        "Product Verification"
    ];
}
