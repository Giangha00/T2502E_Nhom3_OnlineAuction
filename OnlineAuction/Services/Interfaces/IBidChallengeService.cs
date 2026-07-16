namespace OnlineAuction.Services.Interfaces;

public sealed record BidChallengeRequirement(bool IsRequired, string Provider);

public sealed record BidChallengeVerificationResult(bool IsValid, string? Message = null);

public interface IBidChallengeService
{
    Task<BidChallengeRequirement> GetRequirementAsync(
        int userId,
        int bidsInCurrentWindow,
        CancellationToken cancellationToken = default);

    Task RequireChallengeAsync(int userId, string reason, CancellationToken cancellationToken = default);

    Task<BidChallengeVerificationResult> VerifyAsync(
        string? token,
        CancellationToken cancellationToken = default);

    Task ClearRequirementAsync(int userId, CancellationToken cancellationToken = default);
}
