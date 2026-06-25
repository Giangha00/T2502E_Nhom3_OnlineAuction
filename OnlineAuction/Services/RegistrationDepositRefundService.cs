using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class RegistrationDepositRefundService : IRegistrationDepositRefundService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPayPalService _payPalService;
    private readonly ILogger<RegistrationDepositRefundService> _logger;

    public RegistrationDepositRefundService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        ILogger<RegistrationDepositRefundService> logger)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _logger = logger;
    }

    public async Task<RegistrationDepositResult> RefundDepositAsync(
        long depositId,
        CancellationToken cancellationToken = default)
    {
        var deposit = await _dbContext.AuctionRegistrationDeposits
            .FirstOrDefaultAsync(d => d.Id == depositId, cancellationToken);

        if (deposit == null)
        {
            return RegistrationDepositResult.Fail("Không tìm thấy tiền cọc.", 404);
        }

        // Idempotency: nếu đã refund rồi thì không gọi PayPal lần 2
        if (deposit.Status == AuctionRegistrationDepositStatuses.Refunded)
        {
            return RegistrationDepositResult.Ok(
                "Tiền cọc đã được hoàn trước đó.",
                auctionId: deposit.AuctionId,
                depositAmount: deposit.Amount);
        }

        if (deposit.Status != AuctionRegistrationDepositStatuses.Paid)
        {
            return RegistrationDepositResult.Fail(
                "Chỉ có thể hoàn tiền cọc đã thanh toán.");
        }

        if (string.IsNullOrWhiteSpace(deposit.PayPalCaptureId))
        {
            return RegistrationDepositResult.Fail(
                "Không có PayPal capture id để hoàn tiền.");
        }

        var refundResult = await _payPalService.RefundCaptureAsync(
            deposit.PayPalCaptureId,
            deposit.Amount,
            cancellationToken);

        if (!refundResult.Success)
        {
            _logger.LogWarning(
                "Refund deposit failed. DepositId={DepositId}, CaptureId={CaptureId}, Error={Error}",
                deposit.Id,
                deposit.PayPalCaptureId,
                refundResult.ErrorMessage);

            return RegistrationDepositResult.Fail(
                refundResult.ErrorMessage ?? "Hoàn tiền thất bại.");
        }

        deposit.Status = AuctionRegistrationDepositStatuses.Refunded;
        deposit.PayPalRefundId = refundResult.RefundId;
        deposit.RefundedAt = DateTime.UtcNow;
        deposit.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegistrationDepositResult.Ok(
            "Hoàn tiền cọc thành công.",
            auctionId: deposit.AuctionId,
            depositAmount: deposit.Amount);
    }
}