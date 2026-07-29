using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SandboxPayPalWalletService : ISandboxPayPalWalletService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly PayPalSettings _payPalSettings;

    public SandboxPayPalWalletService(
        AuctionHouseDbContext dbContext,
        IOptions<PayPalSettings> payPalSettings)
    {
        _dbContext = dbContext;
        _payPalSettings = payPalSettings.Value;
    }

    /// <summary>
    /// Opt-in demo mode only. Real PayPal checkout must not depend on this ledger.
    /// </summary>
    public bool IsEnforced =>
        _payPalSettings.IsSandbox && _payPalSettings.EnforceSandboxWallet;

    public async Task<decimal> GetBalanceAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!_payPalSettings.IsSandbox)
        {
            return decimal.MaxValue;
        }

        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        return wallet.Balance;
    }

    public async Task<SandboxWalletCheckResult> EnsureSufficientBalanceAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnforced || amount <= 0m)
        {
            return SandboxWalletCheckResult.Ok();
        }

        var balance = await GetBalanceAsync(userId, cancellationToken);
        if (balance < amount)
        {
            return SandboxWalletCheckResult.Fail(
                BuildInsufficientMessage(balance, amount),
                balance);
        }

        return SandboxWalletCheckResult.Ok(balance);
    }

    public async Task<SandboxWalletDeductResult> TryDeductAsync(
        int userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0m)
        {
            return SandboxWalletDeductResult.Ok(0m);
        }

        // When enforcement is off, keep an optional ledger without blocking payment.
        if (!IsEnforced)
        {
            if (!_payPalSettings.IsSandbox)
            {
                return SandboxWalletDeductResult.Ok(0m);
            }

            var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
            if (wallet.Balance >= amount)
            {
                wallet.Balance -= amount;
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            return SandboxWalletDeductResult.Ok(wallet.Balance);
        }

        var enforcedWallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        if (enforcedWallet.Balance < amount)
        {
            return SandboxWalletDeductResult.Fail(
                BuildInsufficientMessage(enforcedWallet.Balance, amount),
                enforcedWallet.Balance);
        }

        enforcedWallet.Balance -= amount;
        enforcedWallet.UpdatedAt = DateTime.UtcNow;
        return SandboxWalletDeductResult.Ok(enforcedWallet.Balance);
    }

    private async Task<UserSandboxWallet> GetOrCreateWalletAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.UserSandboxWallets
            .FirstOrDefaultAsync(item => item.UserId == userId && item.DeletedAt == null, cancellationToken);

        if (wallet is not null)
        {
            return wallet;
        }

        var initialBalance = Math.Max(0m, _payPalSettings.SandboxInitialWalletBalance);
        wallet = new UserSandboxWallet
        {
            UserId = userId,
            Balance = initialBalance,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserSandboxWallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    private string BuildInsufficientMessage(decimal balance, decimal amount) =>
        $"Thanh toán thất bại: số dư ví PayPal sandbox ({balance.ToString("N2")} {_payPalSettings.CurrencyCode}) " +
        $"nhỏ hơn số tiền cần thanh toán ({amount.ToString("N2")} {_payPalSettings.CurrencyCode}).";
}
