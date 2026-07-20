namespace OnlineAuction.Entities;

/// <summary>
/// Simulated PayPal buyer wallet used only when PayPal.Mode = sandbox.
/// PayPal does not expose real buyer balances to merchants.
/// </summary>
public class UserSandboxWallet : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public decimal Balance { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
