namespace OnlineAuction.Models;

public class RegistrationDepositResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int StatusCode { get; init; } = 400;

    public string? ApprovalUrl { get; init; }

    public int? AuctionId { get; init; }

    public decimal? DepositAmount { get; init; }

    public static RegistrationDepositResult Ok(
        string message,
        string? approvalUrl = null,
        int? auctionId = null,
        decimal? depositAmount = null)
    {
        return new RegistrationDepositResult
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            ApprovalUrl = approvalUrl,
            AuctionId = auctionId,
            DepositAmount = depositAmount
        };
    }

    public static RegistrationDepositResult Fail(string message, int statusCode = 400)
    {
        return new RegistrationDepositResult
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}