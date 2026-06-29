using OnlineAuction.Messaging.Messages;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Messaging.Handlers;

public interface IEmailSendMessageHandler
{
    Task HandleAsync(EmailSendMessage message, CancellationToken cancellationToken = default);
}

public sealed class EmailSendMessageHandler : IEmailSendMessageHandler
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<EmailSendMessageHandler> _logger;

    public EmailSendMessageHandler(
        IEmailSender emailSender,
        IEmailVerificationService emailVerificationService,
        ILogger<EmailSendMessageHandler> logger)
    {
        _emailSender = emailSender;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
    }

    public async Task HandleAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        switch (message.Kind)
        {
            case EmailSendKind.PasswordResetOtp:
                if (string.IsNullOrWhiteSpace(message.OtpCode) || message.ExpiryMinutes is null)
                {
                    return;
                }

                var otpSent = await _emailSender.SendPasswordResetOtpAsync(
                    message.To,
                    message.FullName,
                    message.OtpCode,
                    message.ExpiryMinutes.Value,
                    message.Locale,
                    cancellationToken);

                if (!otpSent)
                {
                    _logger.LogWarning("Password reset OTP email failed for {Email}.", message.To);
                }

                break;

            case EmailSendKind.EmailConfirmation:
                if (string.IsNullOrWhiteSpace(message.ConfirmUrl))
                {
                    return;
                }

                var confirmSent = await _emailVerificationService.SendConfirmationAsync(
                    message.To,
                    message.FullName,
                    message.ConfirmUrl,
                    message.Locale,
                    cancellationToken);

                if (!confirmSent)
                {
                    _logger.LogWarning("Email confirmation failed for {Email}.", message.To);
                }

                break;
        }
    }
}
