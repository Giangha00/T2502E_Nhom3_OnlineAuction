namespace OnlineAuction.Models;

public sealed class PlaceBidResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int StatusCode { get; init; } = 400;

    public decimal? CurrentPrice { get; init; }

    public int? BidCount { get; init; }

    public decimal? MinNextBid { get; init; }

    public DateTime? EndDate { get; init; }

    public IReadOnlyList<BidHistoryItemViewModel>? BidHistory { get; init; }

    public static PlaceBidResult Ok(
        string message,
        decimal currentPrice,
        int bidCount,
        decimal minNextBid,
        DateTime endDate,
        IReadOnlyList<BidHistoryItemViewModel> bidHistory) =>
        new()
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            CurrentPrice = currentPrice,
            BidCount = bidCount,
            MinNextBid = minNextBid,
            EndDate = endDate,
            BidHistory = bidHistory
        };

    public static PlaceBidResult Fail(string message, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
}
