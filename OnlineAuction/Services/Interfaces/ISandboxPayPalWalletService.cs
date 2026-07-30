namespace OnlineAuction.Services.Interfaces;

public interface ISandboxPayPalWalletService
{
    /// <summary>
    /// Returns true when opt-in sandbox wallet enforcement is active
    /// (<c>PayPal:Mode=sandbox</c> and <c>PayPal:EnforceSandboxWallet=true</c>).
    /// </summary>
    bool IsEnforced { get; }

    Task<decimal> GetBalanceAsync(int userId, CancellationToken cancellationToken = default);

    Task<SandboxWalletCheckResult> EnsureSufficientBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deducts from the sandbox wallet inside the caller's DbContext/transaction.
    /// When enforcement is off: best-effort ledger only (never fails the payment).
    /// </summary>
    Task<SandboxWalletDeductResult> TryDeductAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default);
}

public sealed class SandboxWalletCheckResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public decimal Balance { get; init; }

    public static SandboxWalletCheckResult Ok(decimal balance = 0m) =>
        new() { Success = true, Balance = balance };

    public static SandboxWalletCheckResult Fail(string message, decimal balance = 0m) =>
        new() { Success = false, ErrorMessage = message, Balance = balance };
}

public sealed class SandboxWalletDeductResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public decimal RemainingBalance { get; init; }

    public static SandboxWalletDeductResult Ok(decimal remainingBalance) =>
        new() { Success = true, RemainingBalance = remainingBalance };

    public static SandboxWalletDeductResult Fail(string message, decimal balance = 0m) =>
        new() { Success = false, ErrorMessage = message, RemainingBalance = balance };
}
