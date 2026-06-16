using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypesAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "products",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "import_price",
                table: "products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_type_id",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "products",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "order_items",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "order_items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "order_items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "order_items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "order_items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "order_items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "bids",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "bids",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "bids",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "bids",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "bids",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "bids",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by",
                table: "auctions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by",
                table: "auctions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by",
                table: "auctions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    slug = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_types_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_product_types_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_product_types_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_users_updated_by",
                table: "users",
                column: "updated_by");

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
                name: "ix_products_product_type_id",
                table: "products",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_updated_by",
                table: "products",
                column: "updated_by");

            migrationBuilder.AddCheckConstraint(
                name: "chk_products_import_price",
                table: "products",
                sql: "`import_price` IS NULL OR `import_price` >= 0");

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
                name: "IX_payments_updated_by",
                table: "payments",
                column: "updated_by");

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
                name: "IX_auctions_updated_by",
                table: "auctions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_created_by",
                table: "product_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_types_deleted_at",
                table: "product_types",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_deleted_by",
                table: "product_types",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_updated_by",
                table: "product_types",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uk_product_types_name",
                table: "product_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_product_types_slug",
                table: "product_types",
                column: "slug",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO product_types (name, slug, sort_order, is_active, created_at)
                SELECT 'Uncategorized', 'uncategorized', 0, 1, UTC_TIMESTAMP()
                WHERE NOT EXISTS (SELECT 1 FROM product_types WHERE slug = 'uncategorized');

                UPDATE products
                SET product_type_id = (SELECT id FROM product_types WHERE slug = 'uncategorized' LIMIT 1)
                WHERE product_type_id = 0;
            ");

            migrationBuilder.AddForeignKey(
                name: "fk_auctions_created_by",
                table: "auctions",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_auctions_deleted_by",
                table: "auctions",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_auctions_updated_by",
                table: "auctions",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bids_created_by",
                table: "bids",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bids_deleted_by",
                table: "bids",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_bids_updated_by",
                table: "bids",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_created_by",
                table: "order_items",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_deleted_by",
                table: "order_items",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_updated_by",
                table: "order_items",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_created_by",
                table: "orders",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_deleted_by",
                table: "orders",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_updated_by",
                table: "orders",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_created_by",
                table: "payments",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_deleted_by",
                table: "payments",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_updated_by",
                table: "payments",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_products_created_by",
                table: "products",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_products_deleted_by",
                table: "products",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_type",
                table: "products",
                column: "product_type_id",
                principalTable: "product_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_updated_by",
                table: "products",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_created_by",
                table: "users",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_deleted_by",
                table: "users",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_updated_by",
                table: "users",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_auctions_created_by",
                table: "auctions");

            migrationBuilder.DropForeignKey(
                name: "fk_auctions_deleted_by",
                table: "auctions");

            migrationBuilder.DropForeignKey(
                name: "fk_auctions_updated_by",
                table: "auctions");

            migrationBuilder.DropForeignKey(
                name: "fk_bids_created_by",
                table: "bids");

            migrationBuilder.DropForeignKey(
                name: "fk_bids_deleted_by",
                table: "bids");

            migrationBuilder.DropForeignKey(
                name: "fk_bids_updated_by",
                table: "bids");

            migrationBuilder.DropForeignKey(
                name: "fk_order_items_created_by",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "fk_order_items_deleted_by",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "fk_order_items_updated_by",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_created_by",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_deleted_by",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_updated_by",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_payments_created_by",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "fk_payments_deleted_by",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "fk_payments_updated_by",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "fk_products_created_by",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_deleted_by",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_product_type",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_updated_by",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_users_created_by",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_users_deleted_by",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_users_updated_by",
                table: "users");

            migrationBuilder.DropTable(
                name: "product_types");

            migrationBuilder.DropIndex(
                name: "IX_users_created_by",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_deleted_at",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_deleted_by",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_updated_by",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_products_created_by",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_deleted_at",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_deleted_by",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_product_type_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_updated_by",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "chk_products_import_price",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_payments_created_by",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payments_deleted_at",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_deleted_by",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_updated_by",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_orders_created_by",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_deleted_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_deleted_by",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_updated_by",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_items_created_by",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_order_items_deleted_at",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_deleted_by",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_updated_by",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_bids_created_by",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "ix_bids_deleted_at",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "IX_bids_deleted_by",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "IX_bids_updated_by",
                table: "bids");

            migrationBuilder.DropIndex(
                name: "IX_auctions_created_by",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "ix_auctions_deleted_at",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "IX_auctions_deleted_by",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "IX_auctions_updated_by",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "products");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "products");

            migrationBuilder.DropColumn(
                name: "import_price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_type_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "products");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "auctions");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "products",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_products_category",
                table: "products",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_amount",
                table: "bids",
                columns: new[] { "auction_id", "amount" });
        }
    }
}
