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

    public bool IsEnforced => _payPalSettings.IsSandbox;

    public async Task<decimal> GetBalanceAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!IsEnforced)
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
        if (!IsEnforced || amount <= 0m)
        {
            return SandboxWalletDeductResult.Ok(0m);
        }

        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        if (wallet.Balance < amount)
        {
            return SandboxWalletDeductResult.Fail(
                BuildInsufficientMessage(wallet.Balance, amount),
                wallet.Balance);
        }

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        return SandboxWalletDeductResult.Ok(wallet.Balance);
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
