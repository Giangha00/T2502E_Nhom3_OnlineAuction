namespace OnlineAuction.Models.PayPal;

public class PayPalCreateOrderResult
{
    public bool Success { get; init; }

    public string? PayPalOrderId { get; init; }

    public string? ApprovalUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalCreateOrderResult Ok(string payPalOrderId, string approvalUrl) =>
        new() { Success = true, PayPalOrderId = payPalOrderId, ApprovalUrl = approvalUrl };

    public static PayPalCreateOrderResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public class PayPalCaptureResult
{
    public bool Success { get; init; }

    public bool AlreadyCaptured { get; init; }

    public string? CaptureId { get; init; }

    public decimal CapturedAmount { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalCaptureResult Ok(string captureId, decimal amount) =>
        new() { Success = true, CaptureId = captureId, CapturedAmount = amount };

    public static PayPalCaptureResult AlreadyDone(string? captureId, decimal amount) =>
        new() { Success = true, AlreadyCaptured = true, CaptureId = captureId, CapturedAmount = amount };

    public static PayPalCaptureResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public class PayPalCheckoutResult
{
    public bool Success { get; init; }

    public string? ApprovalUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalCheckoutResult Ok(string approvalUrl) =>
        new() { Success = true, ApprovalUrl = approvalUrl };

    public static PayPalCheckoutResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public class PayPalCaptureCheckoutResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int PrimaryOrderId { get; init; }

    public IReadOnlyList<int> PaidOrderIds { get; init; } = [];

    public static PayPalCaptureCheckoutResult Ok(int primaryOrderId, IReadOnlyList<int> paidOrderIds) =>
        new() { Success = true, PrimaryOrderId = primaryOrderId, PaidOrderIds = paidOrderIds };

    public static PayPalCaptureCheckoutResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public class PayPalCancelResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalCancelResult Ok() => new() { Success = true };

    public static PayPalCancelResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public class PayPalVerifyWebhookResult
{
    public bool Success { get; init; }

    public bool Verified { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalVerifyWebhookResult Ok() => new() { Success = true, Verified = true };

    public static PayPalVerifyWebhookResult Fail(string message) => new() { Success = false, Verified = false, ErrorMessage = message };
}

public class PayPalWebhookProcessingResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public static PayPalWebhookProcessingResult Ok() => new() { Success = true };

    public static PayPalWebhookProcessingResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public class PayPalRefundResult
{
    // true nếu PayPal refund thành công
    public bool Success { get; init; }

    // Refund id do PayPal trả về
    // Cần lưu vào deposit.PayPalRefundId
    public string? RefundId { get; init; }

    // Trạng thái refund từ PayPal, ví dụ COMPLETED / PENDING
    public string? Status { get; init; }

    // Message lỗi nếu refund thất bại
    public string? ErrorMessage { get; init; }

    public static PayPalRefundResult Ok(string refundId, string status)
    {
        return new PayPalRefundResult
        {
            Success = true,
            RefundId = refundId,
            Status = status
        };
    }

    public static PayPalRefundResult Fail(string message)
    {
        return new PayPalRefundResult
        {
            Success = false,
            ErrorMessage = message
        };
    }
}