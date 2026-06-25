using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IRegistrationDepositRefundService
{
    Task<RegistrationDepositResult> RefundDepositAsync(
        long depositId,
        CancellationToken cancellationToken = default);
    // Refund toàn bộ tiền cọc của người thua trong 1 phiên đấu giá
    // Method này sẽ được gọi tự động khi auction kết thúc
    Task<int> RefundLoserDepositsForAuctionAsync(
        int auctionId,
        CancellationToken cancellationToken = default);
}