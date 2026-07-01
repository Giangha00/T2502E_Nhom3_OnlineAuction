namespace OnlineAuction.Models;

public sealed class ListingFeeCollectionResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public decimal FeeAmount { get; init; }

    public bool AlreadyCollected { get; init; }

    public static ListingFeeCollectionResult Succeeded(decimal feeAmount, bool alreadyCollected = false) =>
        new()
        {
            Success = true,
            FeeAmount = feeAmount,
            AlreadyCollected = alreadyCollected,
            Message = alreadyCollected
                ? "Listing fee was already collected for this auction."
                : "Listing fee collected successfully."
        };

    public static ListingFeeCollectionResult Failed(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
