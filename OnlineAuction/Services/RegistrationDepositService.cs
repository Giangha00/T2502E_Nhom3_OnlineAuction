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
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<RegistrationDepositService> _logger;
    private readonly PlatformFeeSettings _feeSettings;

    public RegistrationDepositService(
        AuctionHouseDbContext dbContext,
        IPayPalService payPalService,
        IPayPalCaptureGuardService payPalCaptureGuardService,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        ILogger<RegistrationDepositService> logger,
        IOptions<PlatformFeeSettings> feeSettings)
    {
        _dbContext = dbContext;
        _payPalService = payPalService;
        _payPalCaptureGuardService = payPalCaptureGuardService;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
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
            return await FailAndNotifyAsync(
                userId,
                "Không tìm thấy phiên đấu giá.",
                auctionId,
                404,
                cancellationToken);
        }

        // Seller không được đăng ký phiên đấu giá của chính mình
        if (auction.Product.SellerId == userId)
        {
            return await FailAndNotifyAsync(
                userId,
                "Seller không được đăng ký phiên đấu giá của chính mình.",
                auctionId,
                403,
                cancellationToken);
        }

        // Auction không yêu cầu registration thì không bắt cọc
        if (!auction.RequiresRegistration)
        {
            return await FailAndNotifyAsync(
                userId,
                "Phiên đấu giá này không yêu cầu đặt cọc.",
                auctionId,
                cancellationToken: cancellationToken);
        }

        // Chỉ cho đăng ký trong khung thời gian đăng ký
        var now = DateTime.UtcNow;
        if (!AuctionScheduleHelper.IsRegistrationOpen(auction, now))
        {
            if (now < DateTimeUtilities.AsUtc(auction.RegistrationStartDate))
            {
                return await FailAndNotifyAsync(
                    userId,
                    "Thời gian đăng ký đấu giá chưa bắt đầu.",
                    auctionId,
                    cancellationToken: cancellationToken);
            }

            if (now >= DateTimeUtilities.AsUtc(auction.RegistrationEndDate))
            {
                return await FailAndNotifyAsync(
                    userId,
                    "Thời gian đăng ký đấu giá đã kết thúc.",
                    auctionId,
                    cancellationToken: cancellationToken);
            }

            return await FailAndNotifyAsync(
                userId,
                "Không nằm trong thời gian đăng ký đấu giá.",
                auctionId,
                cancellationToken: cancellationToken);
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
            return await FailAndNotifyAsync(
                userId,
                ex.Message,
                auctionId,
                cancellationToken: cancellationToken);
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
            return await FailAndNotifyAsync(
                userId,
                "Bạn đã đăng ký phiên đấu giá này rồi.",
                auctionId,
                cancellationToken: cancellationToken);
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
            return await FailAndNotifyAsync(
                userId,
                payPalOrder.ErrorMessage ?? "Không thể tạo PayPal order.",
                auctionId,
                cancellationToken: cancellationToken);
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

        await _notificationService.CreateAndPushAsync(
            userId,
            _notifyLocalizer[NotificationKeys.DepositRequestCreatedTitle],
            _notifyLocalizer[NotificationKeys.DepositRequestCreatedMessage],
            NotificationType.Payment,
            $"/Auction/Detail/{auctionId}",
            NotificationReferenceTypes.AuctionDepositInitiated,
            auctionId,
            debounceWindow: TimeSpan.FromMinutes(2),
            cancellationToken: cancellationToken);

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
            return await FailAndNotifyAsync(
                userId,
                "Thiếu PayPal token.",
                cancellationToken: cancellationToken);
        }

        var deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return await FailAndNotifyAsync(
                userId,
                "Không tìm thấy giao dịch đặt cọc.",
                statusCode: 404,
                cancellationToken: cancellationToken);
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

            return await FailAndNotifyAsync(
                userId,
                "Giao dịch đặt cọc không còn ở trạng thái chờ thanh toán.",
                deposit.AuctionId,
                cancellationToken: cancellationToken);
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
            return await FailAndNotifyAsync(
                userId,
                captureResult.ErrorMessage ?? "Capture PayPal thất bại.",
                deposit.AuctionId,
                cancellationToken: cancellationToken);
        }

        deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return await FailAndNotifyAsync(
                userId,
                "Không tìm thấy giao dịch đặt cọc.",
                statusCode: 404,
                cancellationToken: cancellationToken);
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

            return await FailAndNotifyAsync(
                userId,
                "Giao dịch đặt cọc không còn ở trạng thái chờ thanh toán.",
                deposit.AuctionId,
                cancellationToken: cancellationToken);
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

            return await FailAndNotifyAsync(
                userId,
                "Số tiền PayPal capture không khớp với tiền cọc. Đã khởi tạo hoàn tiền.",
                deposit.AuctionId,
                cancellationToken: cancellationToken);
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

        var productName = auction?.Product?.Name ?? "phiên đấu giá";
        await _notificationService.CreateAndPushAsync(
            userId,
            _notifyLocalizer[NotificationKeys.DepositPaidTitle],
            _notifyLocalizer.Format(NotificationKeys.DepositPaidMessage, productName, deposit.Amount),
            NotificationType.Auction,
            $"/Auction/Detail/{deposit.AuctionId}",
            NotificationReferenceTypes.AuctionRegistrationConfirmed,
            deposit.AuctionId,
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
            return await FailAndNotifyAsync(
                userId,
                "Thiếu PayPal token.",
                cancellationToken: cancellationToken);
        }

        var deposit = await _dbContext.AuctionRegistrationDeposits
            .Include(d => d.Registration)
            .FirstOrDefaultAsync(
                d => d.PayPalOrderId == payPalOrderId && d.UserId == userId,
                cancellationToken);

        if (deposit == null)
        {
            return await FailAndNotifyAsync(
                userId,
                "Không tìm thấy giao dịch đặt cọc.",
                statusCode: 404,
                cancellationToken: cancellationToken);
        }

        if (deposit.Status == AuctionRegistrationDepositStatuses.Paid)
        {
            return await FailAndNotifyAsync(
                userId,
                "Giao dịch đã thanh toán, không thể hủy.",
                deposit.AuctionId,
                cancellationToken: cancellationToken);
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

        await _notificationService.CreateAndPushAsync(
            userId,
            _notifyLocalizer[NotificationKeys.DepositCancelledTitle],
            _notifyLocalizer[NotificationKeys.DepositCancelledMessage],
            NotificationType.Payment,
            $"/Auction/Detail/{deposit.AuctionId}",
            NotificationReferenceTypes.AuctionDepositCancelled,
            deposit.AuctionId,
            debounceWindow: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);

        return RegistrationDepositResult.Ok(
            "Bạn đã hủy thanh toán tiền cọc.",
            auctionId: deposit.AuctionId,
            depositAmount: deposit.Amount);
    }

    private async Task<RegistrationDepositResult> FailAndNotifyAsync(
        int userId,
        string message,
        int? auctionId = null,
        int statusCode = 400,
        CancellationToken cancellationToken = default)
    {
        await NotifyDepositFailureAsync(userId, auctionId, message, cancellationToken);
        return RegistrationDepositResult.Fail(message, statusCode, auctionId);
    }

    private async Task NotifyDepositFailureAsync(
        int userId,
        int? auctionId,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await _notificationService.CreateAndPushAsync(
            userId,
            _notifyLocalizer[NotificationKeys.DepositFailedTitle],
            message,
            NotificationType.Payment,
            auctionId is > 0 ? $"/Auction/Detail/{auctionId.Value}" : null,
            auctionId is > 0 ? NotificationReferenceTypes.AuctionDepositFailed : null,
            auctionId is > 0 ? auctionId : null,
            debounceWindow: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);
    }
}