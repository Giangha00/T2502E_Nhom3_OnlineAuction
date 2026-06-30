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
    private readonly INotificationService _notificationService;
    private readonly ILogger<RegistrationDepositRefundService> _logger;

    public RegistrationDepositRefundService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        INotificationService notificationService,
        ILogger<RegistrationDepositRefundService> logger)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<RegistrationDepositResult> RefundDepositAsync(
        long depositId,
        bool pushNotification = true,
        CancellationToken cancellationToken = default)
    {
        var deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Auction)
                .ThenInclude(a => a.Product)
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

        if (pushNotification)
        {
            var productName = deposit.Auction?.Product?.Name ?? "the auction";
            await _notificationService.CreateAndPushAsync(
                deposit.UserId,
                "Deposit refunded",
                $"Your deposit of ${deposit.Amount:N0} for {productName} has been refunded.",
                NotificationType.Refund,
                $"/Auction/Detail/{deposit.AuctionId}",
                NotificationReferenceTypes.AuctionDepositRefunded,
                deposit.AuctionId,
                cancellationToken: cancellationToken);
        }

        return RegistrationDepositResult.Ok(
            "Hoàn tiền cọc thành công.",
            auctionId: deposit.AuctionId,
            depositAmount: deposit.Amount);
    }
    public async Task<int> RefundLoserDepositsForAuctionAsync(
    int auctionId,
    CancellationToken cancellationToken = default)
{
    // Lấy auction để biết WinnerId.
    // WinnerId được set khi hệ thống tạo order cho người thắng.
    var auction = await _dbContext.Auctions
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

    if (auction == null)
    {
        _logger.LogWarning(
            "Cannot refund deposits because auction {AuctionId} was not found.",
            auctionId);

        return 0;
    }

    // Nếu auction.WinnerId có giá trị:
    // - người đó là winner
    // - không refund tiền cọc cho winner
    //
    // Nếu auction.WinnerId null:
    // - nghĩa là auction không có người thắng
    // - refund tất cả deposit đã paid
    var winnerId = auction.WinnerId;

    // Chỉ lấy deposit đã paid.
    // Không lấy refunded để tránh refund trùng.
    // Không lấy pending/cancelled vì chưa thanh toán hoặc đã hủy.
    var loserDepositIds = await _dbContext.AuctionRegistrationDeposits
        .AsNoTracking()
        .Where(d =>
            d.AuctionId == auctionId &&
            d.Status == AuctionRegistrationDepositStatuses.Paid &&
            d.PayPalCaptureId != null &&
            (!winnerId.HasValue || d.UserId != winnerId.Value))
        .Select(d => d.Id)
        .ToListAsync(cancellationToken);

    var refundedCount = 0;

    foreach (var depositId in loserDepositIds)
    {
        // Gọi lại method RefundDepositAsync đã có.
        // Method này đã xử lý:
        // - kiểm tra deposit paid
        // - gọi PayPal RefundCaptureAsync
        // - lưu paypal_refund_id
        // - chuyển status = refunded
        // - idempotency nếu đã refund rồi
        var result = await RefundDepositAsync(depositId, cancellationToken: cancellationToken);

        if (result.Success)
        {
            refundedCount++;
        }
        else
        {
            // Không throw exception để tránh 1 refund fail làm dừng toàn bộ auction finalization.
            // Log lại để sau này admin/worker có thể retry.
            _logger.LogWarning(
                "Auto refund failed. AuctionId={AuctionId}, DepositId={DepositId}, Message={Message}",
                auctionId,
                depositId,
                result.Message);
        }
    }

    return refundedCount;
}
}