using OnlineAuction.Messaging.Handlers;
using OnlineAuction.Messaging.Messages;

namespace OnlineAuction.Services;

public interface IEmailQueueService
{
    Task<bool> QueuePasswordResetOtpAsync(
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale,
        CancellationToken cancellationToken = default);

    Task<bool> QueueEmailConfirmationAsync(
        string to,
        string fullName,
        string confirmUrl,
        string locale,
        CancellationToken cancellationToken = default);
}

public sealed class EmailQueueService : IEmailQueueService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailQueueService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> QueuePasswordResetOtpAsync(
        string to,
        string fullName,
        string otpCode,
        int expiryMinutes,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var message = new EmailSendMessage
        {
            Kind = EmailSendKind.PasswordResetOtp,
            To = to,
            FullName = fullName,
            OtpCode = otpCode,
            ExpiryMinutes = expiryMinutes,
            Locale = locale
        };

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEmailSendMessageHandler>();
        return await handler.HandleAsync(message, cancellationToken);
    }

    public async Task<bool> QueueEmailConfirmationAsync(
        string to,
        string fullName,
        string confirmUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var message = new EmailSendMessage
        {
            Kind = EmailSendKind.EmailConfirmation,
            To = to,
            FullName = fullName,
            ConfirmUrl = confirmUrl,
            Locale = locale
        };

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEmailSendMessageHandler>();
        return await handler.HandleAsync(message, cancellationToken);
    }
}
