using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    role = table.Column<byte>(type: "tinyint", nullable: false),
                    is_super_admin = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    avatar_url = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    normalized_username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    normalized_email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    security_stamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                    access_failed_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permission",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_role",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_categories_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_categories_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    related_url = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reference_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notifications_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notifications_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notifications_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 45.00m),
                    vault_insurance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    platform_fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    seller_fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    seller_proceeds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    deposit_applied = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending_payment"),
                    order_source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "auction_win"),
                    payment_deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    shipping_full_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    shipping_address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    shipping_city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    shipping_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("chk_orders_amounts", "subtotal > 0 AND total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_orders_buyer",
                        column: x => x.buyer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_device_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    fcm_token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    device_info = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_device_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_device_tokens_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_otp_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    code_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    salt = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    max_attempts = table.Column<int>(type: "int", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_otp_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_otp_codes_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_user_permissions_permission",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_permissions_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    set_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    card_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    grade_label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    year = table.Column<int>(type: "int", nullable: true),
                    language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    short_description = table.Column<string>(type: "text", nullable: true),
                    description_html = table.Column<string>(type: "text", nullable: true),
                    primary_image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_templates_category",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_templates_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_templates_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_templates_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    order_id = table.Column<int>(type: "int", nullable: true),
                    order_reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    complaint_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "refund"),
                    reason_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    requested_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    contact_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    contact_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    resolution_note = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    evidence_urls = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaints", x => x.id);
                    table.CheckConstraint("chk_complaints_status", "status IN ('pending','under_review','approved','rejected','closed')");
                    table.CheckConstraint("chk_complaints_type", "complaint_type IN ('refund','dispute','authenticity','other')");
                    table.ForeignKey(
                        name: "fk_complaints_buyer",
                        column: x => x.buyer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_complaints_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_complaints_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    transaction_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    paypal_order_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("chk_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payments_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    product_template_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    short_description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    subtitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    description_html = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "graded"),
                    product_origin = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    year = table.Column<int>(type: "int", nullable: true),
                    set_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    card_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    grade_label = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    cert_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    grading_centering = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    grading_corners = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    grading_edges = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    grading_surface = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    primary_image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    estimated_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    import_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("chk_products_estimated_value", "estimated_value IS NULL OR estimated_value >= 0");
                    table.CheckConstraint("chk_products_import_price", "import_price IS NULL OR import_price >= 0");
                    table.ForeignKey(
                        name: "fk_products_category",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_products_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_products_product_template",
                        column: x => x.product_template_id,
                        principalTable: "product_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_seller",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    starting_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bid_step = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    current_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    buy_now_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    listing_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "auction"),
                    requires_registration = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "live"),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    verified_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    verified_by = table.Column<int>(type: "int", nullable: true),
                    reject_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    registration_start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    registration_end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    auction_event_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    winner_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auctions", x => x.id);
                    table.CheckConstraint("chk_auctions_dates", "end_date > start_date");
                    table.CheckConstraint("chk_auctions_listing_type", "listing_type IN ('auction', 'buynow')");
                    table.CheckConstraint("chk_auctions_prices", "starting_price > 0 AND bid_step > 0 AND current_price >= 0 AND (buy_now_price IS NULL OR buy_now_price > starting_price)");
                    table.CheckConstraint("chk_auctions_status", "status IN ('pending_review','rejected','scheduled','live','ending_soon','ended','awaiting_payment','completed','cancelled')");
                    table.ForeignKey(
                        name: "fk_auctions_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auctions_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auctions_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auctions_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auctions_verified_by",
                        column: x => x.verified_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auctions_winner",
                        column: x => x.winner_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    file_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_documents_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_documents_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_documents_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_documents_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_images_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_product_images_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_images_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "auction_registrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    registered_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reject_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_registrations", x => x.id);
                    table.CheckConstraint("chk_registrations_status", "status IN ('pending', 'approved', 'rejected', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_registrations_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registrations_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_registrations_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_registrations_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_registrations_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_registrations_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    bidder_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bid_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "manual"),
                    is_winning = table.Column<bool>(type: "bit", nullable: false),
                    placed_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    is_flagged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    flag_reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bids", x => x.id);
                    table.CheckConstraint("chk_bids_amount", "amount > 0");
                    table.CheckConstraint("chk_bids_bid_type", "bid_type IN ('manual', 'buy_now')");
                    table.ForeignKey(
                        name: "fk_bids_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bids_bidder",
                        column: x => x.bidder_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bids_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bids_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bids_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    item_grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    item_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    winning_bid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.CheckConstraint("chk_order_items_winning_bid", "winning_bid > 0");
                    table.ForeignKey(
                        name: "fk_order_items_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_order_items_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_order_items_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_items_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "watchlist_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    added_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_watchlist_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_watchlist_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "winner_non_payment_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    cancelled_order_id = table.Column<int>(type: "int", nullable: false),
                    defaulting_user_id = table.Column<int>(type: "int", nullable: false),
                    forfeited_deposit_id = table.Column<long>(type: "bigint", nullable: true),
                    forfeited_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    second_chance_user_id = table.Column<int>(type: "int", nullable: true),
                    second_chance_order_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_winner_non_payment_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_winner_non_payment_logs_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_registration_deposits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    auction_registration_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    paypal_order_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    paypal_capture_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    paypal_refund_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    forfeited_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_registration_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_auction_registration_deposits_auction_registrations_auction_registration_id",
                        column: x => x.auction_registration_id,
                        principalTable: "auction_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_auction_registration_deposits_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auction_registration_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auction_registration_deposits_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auction_registration_deposits_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auction_registration_deposits_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bid_fraud_alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    bid_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    alert_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "open"),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bid_fraud_alerts", x => x.id);
                    table.CheckConstraint("chk_fraud_alerts_severity", "severity IN ('low','medium','high')");
                    table.CheckConstraint("chk_fraud_alerts_status", "status IN ('open','reviewed','dismissed')");
                    table.ForeignKey(
                        name: "fk_fraud_alerts_auction",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fraud_alerts_bid",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fraud_alerts_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fraud_alerts_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_auction_id",
                table: "auction_registration_deposits",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_auction_registration_id",
                table: "auction_registration_deposits",
                column: "auction_registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_created_by",
                table: "auction_registration_deposits",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_auction_registration_deposits_deleted_at",
                table: "auction_registration_deposits",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_deleted_by",
                table: "auction_registration_deposits",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_updated_by",
                table: "auction_registration_deposits",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registration_deposits_user_id",
                table: "auction_registration_deposits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_paypal_order_id",
                table: "auction_registration_deposits",
                column: "paypal_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registrations_created_by",
                table: "auction_registrations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registrations_deleted_by",
                table: "auction_registrations",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registrations_reviewed_by",
                table: "auction_registrations",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_auction_registrations_updated_by",
                table: "auction_registrations",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_auction_status",
                table: "auction_registrations",
                columns: new[] { "auction_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_registrations_deleted_at",
                table: "auction_registrations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_user_status",
                table: "auction_registrations",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_registrations_auction_user",
                table: "auction_registrations",
                columns: new[] { "auction_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auctions_created_by",
                table: "auctions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_deleted_at",
                table: "auctions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_deleted_by",
                table: "auctions",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_listing_type",
                table: "auctions",
                column: "listing_type");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_product_id",
                table: "auctions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_status_end_date",
                table: "auctions",
                columns: new[] { "status", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_auctions_updated_by",
                table: "auctions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_verified_by",
                table: "auctions",
                column: "verified_by");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_winner_id",
                table: "auctions",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "IX_bid_fraud_alerts_bid_id",
                table: "bid_fraud_alerts",
                column: "bid_id");

            migrationBuilder.CreateIndex(
                name: "IX_bid_fraud_alerts_reviewed_by",
                table: "bid_fraud_alerts",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_bid_fraud_alerts_user_id",
                table: "bid_fraud_alerts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fraud_alerts_auction_created",
                table: "bid_fraud_alerts",
                columns: new[] { "auction_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fraud_alerts_dedup_lookup",
                table: "bid_fraud_alerts",
                columns: new[] { "auction_id", "alert_type", "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fraud_alerts_status_created",
                table: "bid_fraud_alerts",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_ip_address",
                table: "bids",
                columns: new[] { "auction_id", "ip_address" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_placed_at",
                table: "bids",
                columns: new[] { "auction_id", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_bidder_id",
                table: "bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "IX_bids_created_by",
                table: "bids",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_bids_deleted_at",
                table: "bids",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_bids_deleted_by",
                table: "bids",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_bids_updated_by",
                table: "bids",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_categories_created_by",
                table: "categories",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_categories_deleted_at",
                table: "categories",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_categories_deleted_by",
                table: "categories",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_categories_updated_by",
                table: "categories",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_categories_name",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_buyer_id",
                table: "complaints",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_created_by",
                table: "complaints",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_deleted_at",
                table: "complaints",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_deleted_by",
                table: "complaints",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_order_id",
                table: "complaints",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_reviewed_by",
                table: "complaints",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_status_created_at",
                table: "complaints",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_complaints_updated_by",
                table: "complaints",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_complaints_request_reference",
                table: "complaints",
                column: "request_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_by",
                table: "notifications",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_deleted_at",
                table: "notifications",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_deleted_by",
                table: "notifications",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_reference",
                table: "notifications",
                columns: new[] { "reference_type", "reference_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_updated_by",
                table: "notifications",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_created",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_auction_id",
                table: "order_items",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_created_by",
                table: "order_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_deleted_at",
                table: "order_items",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_deleted_by",
                table: "order_items",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_updated_by",
                table: "order_items",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_order_items_order_auction",
                table: "order_items",
                columns: new[] { "order_id", "auction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_buyer_status",
                table: "orders",
                columns: new[] { "buyer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by",
                table: "orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_orders_deleted_at",
                table: "orders",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_orders_deleted_by",
                table: "orders",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_orders_updated_by",
                table: "orders",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_orders_reference",
                table: "orders",
                column: "order_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_created_by",
                table: "payments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payments_deleted_at",
                table: "payments",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_payments_deleted_by",
                table: "payments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_paypal_order_id",
                table: "payments",
                column: "paypal_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payments_transaction_id",
                table: "payments",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_updated_by",
                table: "payments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ux_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_documents_created_by",
                table: "product_documents",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_documents_deleted_at",
                table: "product_documents",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_documents_deleted_by",
                table: "product_documents",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_documents_product_id",
                table: "product_documents",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_documents_updated_by",
                table: "product_documents",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_created_by",
                table: "product_images",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_deleted_at",
                table: "product_images",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_deleted_by",
                table: "product_images",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_updated_by",
                table: "product_images",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_templates_category_id",
                table: "product_templates",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_templates_created_by",
                table: "product_templates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_templates_deleted_at",
                table: "product_templates",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_templates_deleted_by",
                table: "product_templates",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_templates_lookup",
                table: "product_templates",
                columns: new[] { "category_id", "name", "set_name", "card_number", "grade_label" });

            migrationBuilder.CreateIndex(
                name: "IX_product_templates_updated_by",
                table: "product_templates",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_created_by",
                table: "products",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_products_deleted_at",
                table: "products",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_products_deleted_by",
                table: "products",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_template_id",
                table: "products",
                column: "product_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_seller_id",
                table: "products",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_updated_by",
                table: "products",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_RoleId",
                table: "role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_role_permissions_role_permission",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_user_device_tokens_user_id",
                table: "user_device_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_user_device_tokens_fcm_token",
                table: "user_device_tokens",
                column: "fcm_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                table: "user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_user_otp_codes_active_lookup",
                table: "user_otp_codes",
                columns: new[] { "user_id", "purpose", "is_used", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_permissions_user_permission",
                table: "user_permissions",
                columns: new[] { "user_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by",
                table: "users",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_deleted_at",
                table: "users",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_by",
                table: "users",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_users_updated_by",
                table: "users",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_username",
                unique: true,
                filter: "[normalized_username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_items_auction_id",
                table: "watchlist_items",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "ix_watchlist_user_id",
                table: "watchlist_items",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_watchlist_user_auction",
                table: "watchlist_items",
                columns: new[] { "user_id", "auction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_winner_non_payment_logs_auction_id",
                table: "winner_non_payment_logs",
                column: "auction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_registration_deposits");

            migrationBuilder.DropTable(
                name: "bid_fraud_alerts");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "product_documents");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_device_tokens");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_otp_codes");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "watchlist_items");

            migrationBuilder.DropTable(
                name: "winner_non_payment_logs");

            migrationBuilder.DropTable(
                name: "auction_registrations");

            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "auctions");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "product_templates");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
