namespace OnlineAuction.Entities;

/// <summary>
/// Optional simulated PayPal buyer wallet ledger (sandbox only).
/// Does not gate real PayPal payments unless PayPal:EnforceSandboxWallet=true.
/// </summary>
public class UserSandboxWallet : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public decimal Balance { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
