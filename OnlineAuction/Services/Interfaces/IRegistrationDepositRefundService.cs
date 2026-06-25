using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IRegistrationDepositRefundService
{
    Task<RegistrationDepositResult> RefundDepositAsync(
        long depositId,
        CancellationToken cancellationToken = default);
}