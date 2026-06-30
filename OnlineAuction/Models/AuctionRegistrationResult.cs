namespace OnlineAuction.Models;

public sealed class AuctionRegistrationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int StatusCode { get; init; } = 400;

    public string? Status { get; init; }

    public int? RegistrationCount { get; init; }

    public decimal? RefundedAmount { get; init; }

    public static AuctionRegistrationResult Ok(
        string message,
        string status,
        int registrationCount,
        decimal? refundedAmount = null) =>
        new()
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            Status = status,
            RegistrationCount = registrationCount,
            RefundedAmount = refundedAmount
        };

    public static AuctionRegistrationResult Fail(string message, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
}
