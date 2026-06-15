using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;

namespace OnlineAuction.Data;

public class AuctionHouseDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AuctionHouseDbContext(DbContextOptions<AuctionHouseDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Auction> Auctions => Set<Auction>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<AuctionOrder> Orders => Set<AuctionOrder>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentityTables(builder);
        ConfigureUsers(builder);
        ConfigureProducts(builder);
        ConfigureAuctions(builder);
        ConfigureBids(builder);
        ConfigureOrders(builder);
        ConfigureOrderItems(builder);
        ConfigurePayments(builder);
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<IdentityRole<int>>().ToTable("roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<int>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("role_claims");
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(120).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(160).IsRequired();
            entity.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(160);
            entity.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
            entity.Property(u => u.UserName).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(u => u.NormalizedUserName).HasColumnName("normalized_username").HasMaxLength(50);
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(u => u.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(256);
            entity.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(256);
            entity.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(u => u.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(u => u.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
            entity.Property(u => u.Role).HasColumnName("role").HasColumnType("tinyint");
            entity.Property(u => u.Status).HasColumnName("status").HasColumnType("tinyint");
            entity.Property(u => u.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(260);
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("uk_users_email");
            entity.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("uk_users_username");
            entity.HasIndex(u => u.Role).HasDatabaseName("ix_users_role");
        });
    }

    private static void ConfigureProducts(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.SellerId).HasColumnName("seller_id");
            entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(p => p.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
            entity.Property(p => p.ShortDescription).HasColumnName("short_description").HasMaxLength(300);
            entity.Property(p => p.DescriptionHtml).HasColumnName("description_html");
            entity.Property(p => p.Condition).HasColumnName("condition").HasMaxLength(20).IsRequired().HasDefaultValue("graded");
            entity.Property(p => p.Year).HasColumnName("year");
            entity.Property(p => p.SetName).HasColumnName("set_name").HasMaxLength(120);
            entity.Property(p => p.GradeLabel).HasColumnName("grade_label").HasMaxLength(20);
            entity.Property(p => p.CertNumber).HasColumnName("cert_number").HasMaxLength(50);
            entity.Property(p => p.PrimaryImage).HasColumnName("primary_image").HasMaxLength(500).IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(p => p.SellerId).HasDatabaseName("ix_products_seller_id");
            entity.HasIndex(p => p.Category).HasDatabaseName("ix_products_category");

            entity.HasOne(p => p.Seller)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.SellerId)
                .HasConstraintName("fk_products_seller")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuctions(ModelBuilder builder)
    {
        builder.Entity<Auction>(entity =>
        {
            entity.ToTable("auctions");

            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.ProductId).HasColumnName("product_id");
            entity.Property(a => a.StartingPrice).HasColumnName("starting_price").HasPrecision(18, 2);
            entity.Property(a => a.BidStep).HasColumnName("bid_step").HasPrecision(18, 2);
            entity.Property(a => a.CurrentPrice).HasColumnName("current_price").HasPrecision(18, 2);
            entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(AuctionStatuses.Live);
            entity.Property(a => a.StartDate).HasColumnName("start_date");
            entity.Property(a => a.EndDate).HasColumnName("end_date");
            entity.Property(a => a.WinnerId).HasColumnName("winner_id");
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(a => a.ProductId).HasDatabaseName("ix_auctions_product_id");
            entity.HasIndex(a => new { a.Status, a.EndDate }).HasDatabaseName("ix_auctions_status_end_date");
            entity.HasIndex(a => a.WinnerId).HasDatabaseName("ix_auctions_winner_id");

            entity.HasOne(a => a.Product)
                .WithMany(p => p.Auctions)
                .HasForeignKey(a => a.ProductId)
                .HasConstraintName("fk_auctions_product")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Winner)
                .WithMany(u => u.WonAuctions)
                .HasForeignKey(a => a.WinnerId)
                .HasConstraintName("fk_auctions_winner")
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_prices",
                "`starting_price` > 0 AND `bid_step` > 0 AND `current_price` >= 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_dates",
                "`end_date` > `start_date`"));
        });
    }

    private static void ConfigureBids(ModelBuilder builder)
    {
        builder.Entity<Bid>(entity =>
        {
            entity.ToTable("bids");

            entity.Property(b => b.Id).HasColumnName("id");
            entity.Property(b => b.AuctionId).HasColumnName("auction_id");
            entity.Property(b => b.BidderId).HasColumnName("bidder_id");
            entity.Property(b => b.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(b => b.IsWinning).HasColumnName("is_winning");
            entity.Property(b => b.PlacedAt).HasColumnName("placed_at");

            entity.HasIndex(b => new { b.AuctionId, b.PlacedAt }).HasDatabaseName("ix_bids_auction_placed_at");
            entity.HasIndex(b => new { b.AuctionId, b.Amount }).HasDatabaseName("ix_bids_auction_amount");
            entity.HasIndex(b => b.BidderId).HasDatabaseName("ix_bids_bidder_id");

            entity.HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .HasConstraintName("fk_bids_auction")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Bidder)
                .WithMany(u => u.Bids)
                .HasForeignKey(b => b.BidderId)
                .HasConstraintName("fk_bids_bidder")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_bids_amount",
                "`amount` > 0"));
        });
    }

    private static void ConfigureOrders(ModelBuilder builder)
    {
        builder.Entity<AuctionOrder>(entity =>
        {
            entity.ToTable("orders");

            entity.Property(o => o.Id).HasColumnName("id");
            entity.Property(o => o.OrderReference).HasColumnName("order_reference").HasMaxLength(30).IsRequired();
            entity.Property(o => o.BuyerId).HasColumnName("buyer_id");
            entity.Property(o => o.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
            entity.Property(o => o.ShippingFee).HasColumnName("shipping_fee").HasPrecision(18, 2).HasDefaultValue(45.00m);
            entity.Property(o => o.VaultInsurance).HasColumnName("vault_insurance").HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
            entity.Property(o => o.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(OrderStatuses.PendingPayment);
            entity.Property(o => o.PaymentDeadline).HasColumnName("payment_deadline");
            entity.Property(o => o.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(o => o.OrderReference).IsUnique().HasDatabaseName("uk_orders_reference");
            entity.HasIndex(o => new { o.BuyerId, o.Status }).HasDatabaseName("ix_orders_buyer_status");

            entity.HasOne(o => o.Buyer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.BuyerId)
                .HasConstraintName("fk_orders_buyer")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_orders_amounts",
                "`subtotal` > 0 AND `total_amount` > 0"));
        });
    }

    private static void ConfigureOrderItems(ModelBuilder builder)
    {
        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            entity.Property(i => i.Id).HasColumnName("id");
            entity.Property(i => i.OrderId).HasColumnName("order_id");
            entity.Property(i => i.AuctionId).HasColumnName("auction_id");
            entity.Property(i => i.ItemName).HasColumnName("item_name").HasMaxLength(160).IsRequired();
            entity.Property(i => i.ItemGrade).HasColumnName("item_grade").HasMaxLength(20);
            entity.Property(i => i.ItemImageUrl).HasColumnName("item_image_url").HasMaxLength(500);
            entity.Property(i => i.WinningBid).HasColumnName("winning_bid").HasPrecision(18, 2);

            entity.HasIndex(i => new { i.OrderId, i.AuctionId }).IsUnique().HasDatabaseName("uk_order_items_order_auction");
            entity.HasIndex(i => i.AuctionId).HasDatabaseName("ix_order_items_auction_id");

            entity.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .HasConstraintName("fk_order_items_order")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Auction)
                .WithMany(a => a.OrderItems)
                .HasForeignKey(i => i.AuctionId)
                .HasConstraintName("fk_order_items_auction")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_order_items_winning_bid",
                "`winning_bid` > 0"));
        });
    }

    private static void ConfigurePayments(ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.OrderId).HasColumnName("order_id");
            entity.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(PaymentStatuses.Pending);
            entity.Property(p => p.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
            entity.Property(p => p.PaidAt).HasColumnName("paid_at");
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(p => p.OrderId).HasDatabaseName("ix_payments_order_id");
            entity.HasIndex(p => p.Status).HasDatabaseName("ix_payments_status");
            entity.HasIndex(p => p.TransactionId).HasDatabaseName("ix_payments_transaction_id");

            entity.HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .HasConstraintName("fk_payments_order")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_payments_amount",
                "`amount` > 0"));
        });
    }
}
