namespace OnlineAuction.Data;

public static class CreateAuctionMockData
{
    public static IReadOnlyList<string> Categories => MockAuctionData.GetCategoryNames();

    public static readonly IReadOnlyList<string> Conditions =
    [
        "Graded",
        "Raw Near Mint",
        "Raw Excellent",
        "Raw Good"
    ];

    public static readonly IReadOnlyList<string> Grades =
    [
        "PSA 10",
        "PSA 9",
        "PSA 8",
        "BGS 9.5",
        "BGS 9",
        "CGC 8.5",
        "CGC 8",
        "Ungraded"
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
        "Product Certificate",
        "Warranty",
        "Product Verification"
    ];
}
