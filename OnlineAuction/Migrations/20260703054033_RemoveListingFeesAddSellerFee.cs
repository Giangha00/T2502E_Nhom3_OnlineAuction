using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

/// <inheritdoc />
public partial class RemoveListingFeesAddSellerFee : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_fees");

        migrationBuilder.AddColumn<decimal>(
            name: "seller_fee",
            table: "orders",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "seller_fee",
            table: "orders");

        migrationBuilder.CreateTable(
            name: "listing_fees",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                auction_id = table.Column<int>(type: "int", nullable: false),
                CreatedByAdminId = table.Column<int>(type: "int", nullable: true),
                seller_id = table.Column<int>(type: "int", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                created_by = table.Column<int>(type: "int", nullable: true),
                deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                deleted_by = table.Column<int>(type: "int", nullable: true),
                fee_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                fee_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                updated_by = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_listing_fees", x => x.id);
                table.ForeignKey(
                    name: "FK_listing_fees_auctions_auction_id",
                    column: x => x.auction_id,
                    principalTable: "auctions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_listing_fees_users_CreatedByAdminId",
                    column: x => x.CreatedByAdminId,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "FK_listing_fees_users_seller_id",
                    column: x => x.seller_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_listing_fees_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_listing_fees_deleted_by",
                    column: x => x.deleted_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_listing_fees_updated_by",
                    column: x => x.updated_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_listing_fees_auction_id",
            table: "listing_fees",
            column: "auction_id");

        migrationBuilder.CreateIndex(
            name: "ix_listing_fees_auction_status",
            table: "listing_fees",
            columns: new[] { "auction_id", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_listing_fees_created_by",
            table: "listing_fees",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "IX_listing_fees_CreatedByAdminId",
            table: "listing_fees",
            column: "CreatedByAdminId");

        migrationBuilder.CreateIndex(
            name: "ix_listing_fees_deleted_at",
            table: "listing_fees",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "IX_listing_fees_deleted_by",
            table: "listing_fees",
            column: "deleted_by");

        migrationBuilder.CreateIndex(
            name: "IX_listing_fees_seller_id",
            table: "listing_fees",
            column: "seller_id");

        migrationBuilder.CreateIndex(
            name: "IX_listing_fees_updated_by",
            table: "listing_fees",
            column: "updated_by");
    }
}
