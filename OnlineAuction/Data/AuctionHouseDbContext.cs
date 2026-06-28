using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineAuction.Entities;

namespace OnlineAuction.Data;

public class AuctionHouseDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AuctionHouseDbContext(DbContextOptions<AuctionHouseDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Auction> Auctions => Set<Auction>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<AuctionOrder> Orders => Set<AuctionOrder>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<AuctionRegistration> AuctionRegistrations => Set<AuctionRegistration>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<ProductDocument> ProductDocuments => Set<ProductDocument>();
    
    public DbSet<AuctionRegistrationDeposit> AuctionRegistrationDeposits => Set<AuctionRegistrationDeposit>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserOtpCode> UserOtpCodes => Set<UserOtpCode>();

    public DbSet<Complaint> Complaints => Set<Complaint>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        
        base.OnModelCreating(builder);
        ConfigureAuctionRegistrationDeposits(builder);
        ConfigureIdentityTables(builder);
        ConfigureUsers(builder);
        ConfigureCategories(builder);
        ConfigureProducts(builder);
        ConfigureProductImages(builder);
        ConfigureProductDocuments(builder);
        ConfigureAuctions(builder);
        ConfigureAuctionRegistrations(builder);
        ConfigureBids(builder);
        ConfigureOrders(builder);
        ConfigureOrderItems(builder);
        ConfigurePayments(builder);
        ConfigureNotifications(builder);
        ConfigureUserDeviceTokens(builder);
        ConfigurePermissions(builder);
        ConfigureUserOtpCodes(builder);
        ConfigureComplaints(builder);
    }

    private static void ConfigurePermissions(ModelBuilder builder)
    {
        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(p => p.Module).HasColumnName("module").HasMaxLength(50).IsRequired();
            entity.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);

            entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_permissions_code");
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");

            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.Property(rp => rp.RoleId).HasColumnName("role_id");
            entity.Property(rp => rp.PermissionId).HasColumnName("permission_id");

            entity.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .HasConstraintName("fk_role_permissions_role")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .HasConstraintName("fk_role_permissions_permission")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique()
                .HasDatabaseName("ux_role_permissions_role_permission");
        });
    }
    private static void ConfigureAuctionRegistrationDeposits(ModelBuilder builder)
{
    builder.Entity<AuctionRegistrationDeposit>(entity =>
    {
        entity.ToTable("auction_registration_deposits");

        entity.Property(d => d.Id).HasColumnName("id");

        entity.Property(d => d.AuctionId).HasColumnName("auction_id");

        entity.Property(d => d.UserId).HasColumnName("user_id");

        entity.Property(d => d.AuctionRegistrationId)
            .HasColumnName("auction_registration_id");
        
        // Tiền nên set precision để tránh lỗi làm tròn trong database
        entity.Property(d => d.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        entity.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();
        entity.Property(d => d.PayPalOrderId)
            .HasColumnName("paypal_order_id")
            .HasMaxLength(120);

        entity.Property(d => d.PayPalCaptureId)
            .HasColumnName("paypal_capture_id")
            .HasMaxLength(120);

        entity.Property(d => d.PayPalRefundId)
            .HasColumnName("paypal_refund_id")
            .HasMaxLength(120);

        entity.Property(d => d.PaidAt).HasColumnName("paid_at");

        entity.Property(d => d.RefundedAt).HasColumnName("refunded_at");

        // Tìm deposit bằng paypal_order_id khi PayPal return về token
        entity.HasIndex(d => d.PayPalOrderId)
            .HasDatabaseName("ix_deposits_paypal_order_id");

        entity.HasOne(d => d.Auction)
            .WithMany()
            .HasForeignKey(d => d.AuctionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(d => d.Registration)
            .WithMany(r => r.Deposits)
            .HasForeignKey(d => d.AuctionRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureAuditableEntity(entity, "auction_registration_deposits");
    });
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
            entity.Property(u => u.CreatedBy).HasColumnName("created_by");
            entity.Property(u => u.UpdatedBy).HasColumnName("updated_by");
            entity.Property(u => u.DeletedAt).HasColumnName("deleted_at");
            entity.Property(u => u.DeletedBy).HasColumnName("deleted_by");

            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("uk_users_email");
            entity.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("uk_users_username");
            entity.HasIndex(u => u.Role).HasDatabaseName("ix_users_role");
            entity.HasIndex(u => u.DeletedAt).HasDatabaseName("ix_users_deleted_at");

            ConfigureUserAuditForeignKeys(entity, "users");
        });
    }

    private static void ConfigureCategories(ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            entity.Property(t => t.Slug).HasColumnName("slug").HasMaxLength(60).IsRequired();
            entity.Property(t => t.SortOrder).HasColumnName("sort_order");
            entity.Property(t => t.IsActive).HasColumnName("is_active");

            entity.HasIndex(t => t.Name).IsUnique().HasDatabaseName("uk_categories_name");
            entity.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("uk_categories_slug");

            ConfigureAuditableEntity(entity, "categories");
        });
    }

    private static void ConfigureProducts(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.SellerId).HasColumnName("seller_id");
            entity.Property(p => p.CategoryId).HasColumnName("category_id");
            entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(p => p.ShortDescription).HasColumnName("short_description").HasMaxLength(300);
            entity.Property(p => p.Subtitle).HasColumnName("subtitle").HasMaxLength(160);
            entity.Property(p => p.DescriptionHtml).HasColumnName("description_html");
            entity.Property(p => p.Condition).HasColumnName("condition").HasMaxLength(20).IsRequired().HasDefaultValue("graded");
            entity.Property(p => p.ProductOrigin).HasColumnName("product_origin").HasMaxLength(120);
            entity.Property(p => p.Year).HasColumnName("year");
            entity.Property(p => p.SetName).HasColumnName("set_name").HasMaxLength(120);
            entity.Property(p => p.Language).HasColumnName("language").HasMaxLength(20);
            entity.Property(p => p.CardNumber).HasColumnName("card_number").HasMaxLength(30);
            entity.Property(p => p.GradeLabel).HasColumnName("grade_label").HasMaxLength(20);
            entity.Property(p => p.CertNumber).HasColumnName("cert_number").HasMaxLength(50);
            entity.Property(p => p.GradingCentering).HasColumnName("grading_centering").HasMaxLength(10);
            entity.Property(p => p.GradingCorners).HasColumnName("grading_corners").HasMaxLength(10);
            entity.Property(p => p.GradingEdges).HasColumnName("grading_edges").HasMaxLength(10);
            entity.Property(p => p.GradingSurface).HasColumnName("grading_surface").HasMaxLength(10);
            entity.Property(p => p.PrimaryImage).HasColumnName("primary_image").HasMaxLength(500).IsRequired();
            entity.Property(p => p.EstimatedValue).HasColumnName("estimated_value").HasPrecision(18, 2);
            entity.Property(p => p.ImportPrice).HasColumnName("import_price").HasPrecision(18, 2);

            entity.HasIndex(p => p.SellerId).HasDatabaseName("ix_products_seller_id");
            entity.HasIndex(p => p.CategoryId).HasDatabaseName("ix_products_category_id");

            entity.HasOne(p => p.Seller)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.SellerId)
                .HasConstraintName("fk_products_seller")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Category)
                .WithMany(t => t.Products)
                .HasForeignKey(p => p.CategoryId)
                .HasConstraintName("fk_products_category")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_products_import_price",
                "`import_price` IS NULL OR `import_price` >= 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_products_estimated_value",
                "`estimated_value` IS NULL OR `estimated_value` >= 0"));

            ConfigureAuditableEntity(entity, "products");
        });
    }

    private static void ConfigureProductImages(ModelBuilder builder)
    {
        builder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");

            entity.Property(i => i.Id).HasColumnName("id");
            entity.Property(i => i.ProductId).HasColumnName("product_id");
            entity.Property(i => i.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
            entity.Property(i => i.SortOrder).HasColumnName("sort_order");

            entity.HasIndex(i => i.ProductId).HasDatabaseName("ix_product_images_product_id");

            entity.HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .HasConstraintName("fk_product_images_product")
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureAuditableEntity(entity, "product_images");
        });
    }

    private static void ConfigureProductDocuments(ModelBuilder builder)
    {
        builder.Entity<ProductDocument>(entity =>
        {
            entity.ToTable("product_documents");

            entity.Property(d => d.Id).HasColumnName("id");
            entity.Property(d => d.ProductId).HasColumnName("product_id");
            entity.Property(d => d.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(d => d.FileUrl).HasColumnName("file_url").HasMaxLength(500).IsRequired();
            entity.Property(d => d.FileType).HasColumnName("file_type").HasMaxLength(20).IsRequired();

            entity.HasIndex(d => d.ProductId).HasDatabaseName("ix_product_documents_product_id");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_product_documents_product")
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureAuditableEntity(entity, "product_documents");
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
            entity.Property(a => a.BuyNowPrice).HasColumnName("buy_now_price").HasPrecision(18, 2);
            entity.Property(a => a.ListingType).HasColumnName("listing_type").HasMaxLength(20).IsRequired().HasDefaultValue(ListingTypes.Auction);
            entity.Property(a => a.RequiresRegistration).HasColumnName("requires_registration").HasDefaultValue(true);
            entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(AuctionStatuses.Live);
            entity.Property(a => a.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(a => a.VerifiedAt).HasColumnName("verified_at");
            entity.Property(a => a.VerifiedBy).HasColumnName("verified_by");
            entity.Property(a => a.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);
            entity.Property(a => a.StartDate).HasColumnName("start_date");
            entity.Property(a => a.EndDate).HasColumnName("end_date");
            entity.Property(a => a.AuctionEventName).HasColumnName("auction_event_name").HasMaxLength(160);
            entity.Property(a => a.WinnerId).HasColumnName("winner_id");

            entity.HasIndex(a => a.ProductId).HasDatabaseName("ix_auctions_product_id");
            entity.HasIndex(a => new { a.Status, a.EndDate }).HasDatabaseName("ix_auctions_status_end_date");
            entity.HasIndex(a => a.ListingType).HasDatabaseName("ix_auctions_listing_type");

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

            entity.HasOne(a => a.Verifier)
                .WithMany()
                .HasForeignKey(a => a.VerifiedBy)
                .HasConstraintName("fk_auctions_verified_by")
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_prices",
                "`starting_price` > 0 AND `bid_step` > 0 AND `current_price` >= 0 AND (`buy_now_price` IS NULL OR `buy_now_price` > `starting_price`)"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_dates",
                "`end_date` > `start_date`"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_listing_type",
                "`listing_type` IN ('auction', 'buynow')"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_auctions_status",
                "`status` IN ('pending_review','rejected','scheduled','live','ending_soon','ended','awaiting_payment','completed','cancelled')"));

            ConfigureAuditableEntity(entity, "auctions");
        });
    }

    private static void ConfigureAuctionRegistrations(ModelBuilder builder)
    {
        builder.Entity<AuctionRegistration>(entity =>
        {
            entity.ToTable("auction_registrations");

            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.AuctionId).HasColumnName("auction_id");
            entity.Property(r => r.UserId).HasColumnName("user_id");
            entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(AuctionRegistrationStatuses.Pending);
            entity.Property(r => r.RegisteredAt).HasColumnName("registered_at");
            entity.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(r => r.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(r => r.RejectReason).HasColumnName("reject_reason").HasMaxLength(300);

            entity.HasIndex(r => new { r.AuctionId, r.UserId }).IsUnique().HasDatabaseName("uk_registrations_auction_user");
            entity.HasIndex(r => new { r.AuctionId, r.Status }).HasDatabaseName("ix_registrations_auction_status");
            entity.HasIndex(r => new { r.UserId, r.Status }).HasDatabaseName("ix_registrations_user_status");

            entity.HasOne(r => r.Auction)
                .WithMany(a => a.Registrations)
                .HasForeignKey(r => r.AuctionId)
                .HasConstraintName("fk_registrations_auction")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.User)
                .WithMany(u => u.AuctionRegistrations)
                .HasForeignKey(r => r.UserId)
                .HasConstraintName("fk_registrations_user")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewedBy)
                .HasConstraintName("fk_registrations_reviewed_by")
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_registrations_status",
                "`status` IN ('pending', 'approved', 'rejected', 'cancelled')"));

            ConfigureAuditableEntity(entity, "registrations");
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
            entity.Property(b => b.BidType).HasColumnName("bid_type").HasMaxLength(20).IsRequired().HasDefaultValue(BidTypes.Manual);
            entity.Property(b => b.IsWinning).HasColumnName("is_winning");
            entity.Property(b => b.PlacedAt).HasColumnName("placed_at");

            entity.HasIndex(b => new { b.AuctionId, b.PlacedAt }).HasDatabaseName("ix_bids_auction_placed_at");
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

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_bids_bid_type",
                "`bid_type` IN ('manual', 'buy_now')"));

            ConfigureAuditableEntity(entity, "bids");
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
            entity.Property(o => o.OrderSource).HasColumnName("order_source").HasMaxLength(20).IsRequired().HasDefaultValue(OrderSources.AuctionWin);
            entity.Property(o => o.PaymentDeadline).HasColumnName("payment_deadline");
            entity.Property(o => o.DepositApplied).HasColumnName("deposit_applied").HasPrecision(18, 2);

            entity.Property(o => o.ShippingFullName).HasColumnName("shipping_full_name").HasMaxLength(120);
            entity.Property(o => o.ShippingAddress).HasColumnName("shipping_address").HasMaxLength(300);
            entity.Property(o => o.ShippingCity).HasColumnName("shipping_city").HasMaxLength(100);
            entity.Property(o => o.ShippingPhone).HasColumnName("shipping_phone").HasMaxLength(20);
            entity.Property(o => o.PaymentMethod).HasColumnName("payment_method").HasMaxLength(30);

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

            ConfigureAuditableEntity(entity, "orders");
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

            ConfigureAuditableEntity(entity, "order_items");
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
            entity.Property(p => p.PayPalOrderId).HasColumnName("paypal_order_id").HasMaxLength(50);
            entity.Property(p => p.PaidAt).HasColumnName("paid_at");

            entity.HasIndex(p => p.OrderId).HasDatabaseName("ix_payments_order_id");
            entity.HasIndex(p => p.PayPalOrderId).HasDatabaseName("ix_payments_paypal_order_id");
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

            ConfigureAuditableEntity(entity, "payments");
        });
    }

    private static void ConfigureNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.Property(n => n.Id).HasColumnName("id");
            entity.Property(n => n.UserId).HasColumnName("user_id");
            entity.Property(n => n.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(n => n.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
            entity.Property(n => n.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
            entity.Property(n => n.RelatedUrl).HasColumnName("related_url").HasMaxLength(260);
            entity.Property(n => n.IsRead).HasColumnName("is_read");
            entity.Property(n => n.ReadAt).HasColumnName("read_at");
            entity.Property(n => n.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
            entity.Property(n => n.ReferenceId).HasColumnName("reference_id");

            entity.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("ix_notifications_user_read");
            entity.HasIndex(n => new { n.UserId, n.CreatedAt }).HasDatabaseName("ix_notifications_user_created");
            entity.HasIndex(n => new { n.ReferenceType, n.ReferenceId, n.UserId })
                .HasDatabaseName("ix_notifications_reference");

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .HasConstraintName("fk_notifications_user")
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureAuditableEntity(entity, "notifications");
        });
    }

    private static void ConfigureUserDeviceTokens(ModelBuilder builder)
    {
        builder.Entity<UserDeviceToken>(entity =>
        {
            entity.ToTable("user_device_tokens");

            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.UserId).HasColumnName("user_id");
            entity.Property(t => t.FcmToken).HasColumnName("fcm_token").HasMaxLength(512).IsRequired();
            entity.Property(t => t.DeviceInfo).HasColumnName("device_info").HasMaxLength(260);
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.LastUsedAt).HasColumnName("last_used_at");

            entity.HasIndex(t => t.FcmToken).IsUnique().HasDatabaseName("uk_user_device_tokens_fcm_token");
            entity.HasIndex(t => t.UserId).HasDatabaseName("ix_user_device_tokens_user_id");

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .HasConstraintName("fk_user_device_tokens_user")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserOtpCodes(ModelBuilder builder)
    {
        builder.Entity<UserOtpCode>(entity =>
        {
            entity.ToTable("user_otp_codes");

            entity.Property(otp => otp.Id).HasColumnName("id");
            entity.Property(otp => otp.UserId).HasColumnName("user_id");
            entity.Property(otp => otp.CodeHash).HasColumnName("code_hash").HasMaxLength(128).IsRequired();
            entity.Property(otp => otp.Salt).HasColumnName("salt").HasMaxLength(64).IsRequired();
            entity.Property(otp => otp.Purpose).HasColumnName("purpose").HasMaxLength(40).IsRequired();
            entity.Property(otp => otp.ExpiresAt).HasColumnName("expires_at");
            entity.Property(otp => otp.AttemptCount).HasColumnName("attempt_count");
            entity.Property(otp => otp.MaxAttempts).HasColumnName("max_attempts");
            entity.Property(otp => otp.IsUsed).HasColumnName("is_used");
            entity.Property(otp => otp.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(otp => new { otp.UserId, otp.Purpose, otp.IsUsed, otp.ExpiresAt })
                .HasDatabaseName("ix_user_otp_codes_active_lookup");

            entity.HasOne(otp => otp.User)
                .WithMany()
                .HasForeignKey(otp => otp.UserId)
                .HasConstraintName("fk_user_otp_codes_user")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureComplaints(ModelBuilder builder)
    {
        builder.Entity<Complaint>(entity =>
        {
            entity.ToTable("complaints");

            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.RequestReference).HasColumnName("request_reference").HasMaxLength(40).IsRequired();
            entity.Property(c => c.OrderId).HasColumnName("order_id");
            entity.Property(c => c.OrderReference).HasColumnName("order_reference").HasMaxLength(30);
            entity.Property(c => c.BuyerId).HasColumnName("buyer_id");
            entity.Property(c => c.ComplaintType).HasColumnName("complaint_type").HasMaxLength(20).IsRequired().HasDefaultValue(ComplaintTypes.Refund);
            entity.Property(c => c.ReasonCode).HasColumnName("reason_code").HasMaxLength(40).IsRequired();
            entity.Property(c => c.Description).HasColumnName("description").HasColumnType("text").IsRequired();
            entity.Property(c => c.RequestedAmount).HasColumnName("requested_amount").HasPrecision(18, 2);
            entity.Property(c => c.ContactName).HasColumnName("contact_name").HasMaxLength(120).IsRequired();
            entity.Property(c => c.ContactEmail).HasColumnName("contact_email").HasMaxLength(256).IsRequired();
            entity.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue(ComplaintStatuses.Pending);
            entity.Property(c => c.AdminNotes).HasColumnName("admin_notes").HasColumnType("text");
            entity.Property(c => c.ResolutionNote).HasColumnName("resolution_note").HasColumnType("text");
            entity.Property(c => c.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(c => c.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(c => c.EvidenceUrlsJson).HasColumnName("evidence_urls").HasColumnType("text");

            entity.HasIndex(c => c.RequestReference).IsUnique().HasDatabaseName("uk_complaints_request_reference");
            entity.HasIndex(c => c.BuyerId).HasDatabaseName("ix_complaints_buyer_id");
            entity.HasIndex(c => c.OrderId).HasDatabaseName("ix_complaints_order_id");
            entity.HasIndex(c => new { c.Status, c.CreatedAt }).HasDatabaseName("ix_complaints_status_created_at");

            entity.HasOne(c => c.Order)
                .WithMany()
                .HasForeignKey(c => c.OrderId)
                .HasConstraintName("fk_complaints_order")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .HasConstraintName("fk_complaints_buyer")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Reviewer)
                .WithMany()
                .HasForeignKey(c => c.ReviewedBy)
                .HasConstraintName("fk_complaints_reviewer")
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_complaints_status",
                "`status` IN ('pending','under_review','approved','rejected','closed')"));

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_complaints_type",
                "`complaint_type` IN ('refund','dispute','authenticity','other')"));

            ConfigureAuditableEntity(entity, "complaints");
        });
    }

    private static void ConfigureAuditableEntity<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        string tableName) where TEntity : AuditableEntity
    {
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");

        entity.HasIndex(e => e.DeletedAt).HasDatabaseName($"ix_{tableName}_deleted_at");

        ConfigureUserAuditForeignKeys(entity, tableName);
    }

    private static void ConfigureUserAuditForeignKeys<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        string tableName) where TEntity : class
    {
        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(nameof(AuditableEntity.CreatedBy))
            .HasConstraintName($"fk_{tableName}_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(nameof(AuditableEntity.UpdatedBy))
            .HasConstraintName($"fk_{tableName}_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(nameof(AuditableEntity.DeletedBy))
            .HasConstraintName($"fk_{tableName}_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
