namespace OnlineAuction.Services.Interfaces;

public interface IEmailVerificationService
{
    Task<bool> SendConfirmationAsync(
        string to,
        string fullName,
        string confirmUrl,
        string locale,
        CancellationToken cancellationToken = default);
}
