using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class RegistrationDepositService : IRegistrationDepositService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPayPalService _payPalService;
    private readonly IPayPalCaptureGuardService _payPalCaptureGuardService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RegistrationDepositService> _logger;
    private readonly PlatformFeeSettings _feeSettings;

    public RegistrationDepositService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        IPayPalCaptureGuardService payPalCaptureGuardService,
        INotificationService notificationService,
        ILogger<RegistrationDepositService> logger,
        IOptions<PlatformFeeSettings> feeSettings)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _payPalCaptureGuardService = payPalCaptureGuardService;
        _notificationService = notificationService;
        _logger = logger;
        _feeSettings = feeSettings.Value;
    }

    public decimal CalculateDepositAmount(decimal? estimatedValue, decimal startingPrice)
    {
        var productValue = estimatedValue ?? startingPrice;
        return MarketplaceFeeCalculator.CalculateRegistrationDeposit(productValue, _feeSettings);
    }

    public async Task<RegistrationDepositResult> InitiateDepositAsync(
        int auctionId,
        int userId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction == null)
        {
            return RegistrationDepositResult.Fail("Không tìm thấy phiên đấu giá.", 404);
        }

        // Seller không được đăng ký phiên đấu giá của chính mình
        if (auction.Product.SellerId == userId)
        {
            return RegistrationDepositResult.Fail(
                "Seller không được đăng ký phiên đấu giá của chính mình.",
                403);
        }

        // Auction không yêu cầu registration thì không bắt cọc
        if (!auction.RequiresRegistration)
        {
            return RegistrationDepositResult.Fail(
                "Phiên đấu giá này không yêu cầu đặt cọc.");
        }

        // Chỉ cho đăng ký trong khung thời gian đăng ký
        var now = DateTime.UtcNow;
        if (!AuctionScheduleHelper.IsRegistrationOpen(auction, now))
        {
            if (now < DateTimeUtilities.AsUtc(auction.RegistrationStartDate))
            {
                return RegistrationDepositResult.Fail(
                    "Thời gian đăng ký đấu giá chưa bắt đầu.");
            }

            if (now >= DateTimeUtilities.AsUtc(auction.RegistrationEndDate))
            {
                return RegistrationDepositResult.Fail(
                    "Thời gian đăng ký đấu giá đã kết thúc.");
            }

            return RegistrationDepositResult.Fail(
                "Không nằm trong thời gian đăng ký đấu giá.");
        }

        decimal depositAmount;

        try
        {
            // Tính tiền cọc tập trung tại service
            depositAmount = CalculateDepositAmount(
                auction.Product.EstimatedValue,
                auction.StartingPrice);
        }
        catch (InvalidOperationException ex)
        {
            return RegistrationDepositResult.Fail(ex.Message);
        }

        var registration = await _dbContext.AuctionRegistrations
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(
                r => r.AuctionId == auctionId && r.UserId == userId,
                cancellationToken);

        // Nếu đã approved rồi thì không tạo deposit mới
        if (registration != null &&
            registration.Status == AuctionRegistrationStatuses.Approved)
        {
            return RegistrationDepositResult.Fail(
                "Bạn đã đăng ký phiên đấu giá này rồi.");
        }

        // Tạo reference gửi sang PayPal
        var referenceId =
            $"auction-deposit-{auctionId}-{userId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // Tạo PayPal checkout order với đúng số tiền cọc
        var payPalOrder = await _payPalService.CreateCheckoutOrderAsync(
            depositAmount,
            referenceId,
            returnUrl,
            cancelUrl,
            cancellationToken);

        if (!payPalOrder.Success ||
            string.IsNullOrWhiteSpace(payPalOrder.PayPalOrderId) ||
            string.IsNullOrWhiteSpace(payPalOrder.ApprovalUrl))
        {
            return RegistrationDepositResult.Fail(
                payPalOrder.ErrorMessage ?? "Không thể tạo PayPal order.");
        }

        // Nếu có deposit pending cũ thì hủy đi để tránh nhiều order pending
        if (registration != null)
        {
            foreach (var oldDeposit in registration.Deposits
                         .Where(d => d.Status == AuctionRegistrationDepositStatuses.Pending))
            {
                oldDeposit.Status = AuctionRegistrationDepositStatuses.Cancelled;
                oldDeposit.UpdatedAt = now;
            }
        }

        if (registration == null)
        {
            registration = new AuctionRegistration
            {
                AuctionId = auctionId,
                UserId = userId,

                // Chưa thanh toán nên pending
                Status = AuctionRegistrationStatuses.Pending,

                RegisteredAt = now,
                CreatedAt = now
            };

            _dbContext.AuctionRegistrations.Add(registration);

            // Save trước để có registration.Id
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Cho phép user thử lại nếu registration cũ cancelled/rejected/pending
            registration.Status = AuctionRegistrationStatuses.Pending;
            registration.RegisteredAt = now;
            registration.ReviewedAt = null;
            registration.RejectReason = null;
            registration.UpdatedAt = now;
        }

        var deposit = new AuctionRegistrationDeposit
        {
            AuctionId = auctionId,
            UserId = userId,
            AuctionRegistrationId = registration.Id,

            // Lưu cố định amount tại thời điểm initiate
            Amount = depositAmount,

            Status = AuctionRegistrationDepositStatuses.Pending,

            // PayPal return token chính là order id này
            PayPalOrderId = payPalOrder.PayPalOrderId,

            CreatedAt = now
        };

        _dbContext.AuctionRegistrationDeposits.Add(deposit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegistrationDepositResult.Ok(
            "Đã tạo yêu cầu đặt cọc. Vui lòng thanh toán qua PayPal.",
            payPalOrder.ApprovalUrl,
            auctionId,
            depositAmount);
    }

    public async Task<RegistrationDepositResult> CaptureDepositAsync(
        int userId,
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return RegistrationDepositResult.Fail("Thiếu PayPal token.");
        }

        var deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return RegistrationDepositResult.Fail(
                "Không tìm thấy giao dịch đặt cọc.",
                404);
        }

        if (deposit.Status == AuctionRegistrationDepositStatuses.Paid)
        {
            return RegistrationDepositResult.Ok(
                "Bạn đã đặt cọc thành công trước đó.",
                auctionId: deposit.AuctionId,
                depositAmount: deposit.Amount);
        }

        if (deposit.Status == AuctionRegistrationDepositStatuses.Applied)
        {
            return RegistrationDepositResult.Ok(
                "Tiền cọc đã được sử dụng cho đơn hàng.",
                auctionId: deposit.AuctionId,
                depositAmount: deposit.Amount);
        }

        if (deposit.Status != AuctionRegistrationDepositStatuses.Pending)
        {
            _logger.LogWarning(
                "Deposit capture rejected because local state is not pending. DepositId={DepositId} Status={Status} PayPalOrderId={PayPalOrderId}",
                deposit.Id,
                deposit.Status,
                payPalOrderId);

            return RegistrationDepositResult.Fail(
                "Giao dịch đặt cọc không còn ở trạng thái chờ thanh toán.");
        }

        var captureContext = new PayPalCaptureContext(
            Flow: "deposit",
            UserId: userId,
            DepositId: deposit.Id);

        var captureResult = await _payPalCaptureGuardService.SafeCaptureAsync(
            payPalOrderId,
            deposit.Amount,
            captureContext,
            cancellationToken);

        if (!captureResult.Success || string.IsNullOrWhiteSpace(captureResult.CaptureId))
        {
            return RegistrationDepositResult.Fail(
                captureResult.ErrorMessage ?? "Capture PayPal thất bại.");
        }

        deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return RegistrationDepositResult.Fail(
                "Không tìm thấy giao dịch đặt cọc.",
                404);
        }

        if (deposit.Status == AuctionRegistrationDepositStatuses.Paid)
        {
            return RegistrationDepositResult.Ok(
                "Bạn đã đặt cọc thành công trước đó.",
                auctionId: deposit.AuctionId,
                depositAmount: deposit.Amount);
        }

        if (deposit.Status != AuctionRegistrationDepositStatuses.Pending)
        {
            var recovery = await _payPalService.RefundCaptureAsync(
                captureResult.CaptureId,
                captureResult.CapturedAmount,
                cancellationToken);

            _logger.LogCritical(
                "MANUAL_RECOVERY_REQUIRED PayPal deposit capture could not be persisted. DepositId={DepositId} PayPalOrderId={PayPalOrderId} CaptureId={CaptureId} RefundSucceeded={RefundSucceeded} RefundError={RefundError}",
                deposit.Id,
                payPalOrderId,
                captureResult.CaptureId,
                recovery.Success,
                recovery.ErrorMessage);

            return RegistrationDepositResult.Fail(
                "Giao dịch đặt cọc không còn ở trạng thái chờ thanh toán.");
        }

        if (!PayPalAmountHelper.AmountsMatch(deposit.Amount, captureResult.CapturedAmount))
        {
            var recovery = await _payPalService.RefundCaptureAsync(
                captureResult.CaptureId,
                captureResult.CapturedAmount,
                cancellationToken);

            _logger.LogCritical(
                "MANUAL_RECOVERY_REQUIRED PayPal deposit amount changed before persistence. DepositId={DepositId} PayPalOrderId={PayPalOrderId} Expected={Expected} Captured={Captured} RefundSucceeded={RefundSucceeded}",
                deposit.Id,
                payPalOrderId,
                deposit.Amount,
                captureResult.CapturedAmount,
                recovery.Success);

            return RegistrationDepositResult.Fail(
                "Số tiền PayPal capture không khớp với tiền cọc. Đã khởi tạo hoàn tiền.");
        }

        var now = DateTime.UtcNow;

        deposit.Status = AuctionRegistrationDepositStatuses.Paid;
        deposit.PayPalCaptureId = captureResult.CaptureId;
        deposit.PaidAt = now;
        deposit.UpdatedAt = now;

        deposit.Registration.Status = AuctionRegistrationStatuses.Approved;
        deposit.Registration.ReviewedAt = now;
        deposit.Registration.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == deposit.AuctionId, cancellationToken);

        var productName = auction?.Product?.Name ?? "the auction";
        await _notificationService.CreateAndPushAsync(
            userId,
            "Registration confirmed",
            $"Your registration for {productName} is confirmed. Deposit of ${deposit.Amount:N0} was received.",
            NotificationType.Auction,
            $"/Auction/Detail/{deposit.AuctionId}",
            cancellationToken: cancellationToken);

        return RegistrationDepositResult.Ok(
            "Đặt cọc thành công. Bạn đã được duyệt đăng ký đấu giá.",
            auctionId: deposit.AuctionId,
            depositAmount: deposit.Amount);
    }

    public async Task<RegistrationDepositResult> CancelDepositAsync(
        int userId,
        string payPalOrderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            return RegistrationDepositResult.Fail("Thiếu PayPal token.");
        }

        var deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return RegistrationDepositResult.Fail(
                "Không tìm thấy giao dịch đặt cọc.",
                404);
        }

        if (deposit.Status == AuctionRegistrationDepositStatuses.Paid)
        {
            return RegistrationDepositResult.Fail(
                "Giao dịch đã thanh toán, không thể hủy.");
        }

        var cancelResult = await _payPalService.CancelOrderAsync(payPalOrderId, cancellationToken);
        if (!cancelResult.Success)
        {
            _logger.LogWarning(
                "PayPal deposit cancel failed for order {PayPalOrderId}: {ErrorMessage}",
                payPalOrderId,
                cancelResult.ErrorMessage);
        }

        var now = DateTime.UtcNow;

        deposit.Status = AuctionRegistrationDepositStatuses.Cancelled;
        deposit.UpdatedAt = now;

        // Rule chọn: registration cancelled để user có thể thử lại
        deposit.Registration.Status = AuctionRegistrationStatuses.Cancelled;
        deposit.Registration.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegistrationDepositResult.Ok(
            "Bạn đã hủy thanh toán tiền cọc.",
            auctionId: deposit.AuctionId,
            depositAmount: deposit.Amount);
    }
}