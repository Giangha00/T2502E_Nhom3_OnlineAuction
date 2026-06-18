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
