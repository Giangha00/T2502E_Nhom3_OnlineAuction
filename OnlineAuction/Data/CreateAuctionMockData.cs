namespace OnlineAuction.Data;

public static class CreateAuctionMockData
{
    public static IReadOnlyList<string> Categories => MockAuctionData.GetCategoryNames();

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
