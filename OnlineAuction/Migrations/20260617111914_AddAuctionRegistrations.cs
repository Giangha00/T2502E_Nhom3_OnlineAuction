using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_registration",
                table: "auctions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "auction_registrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "pending")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reject_reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_registrations", x => x.id);
                    table.CheckConstraint("chk_registrations_status", "`status` IN ('pending', 'approved', 'rejected', 'cancelled')");
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_registrations_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_registrations_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_registrations_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_registrations_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_listing_type",
                table: "auctions",
                column: "listing_type");

            migrationBuilder.AddCheckConstraint(
                name: "chk_auctions_listing_type",
                table: "auctions",
                sql: "`listing_type` IN ('auction', 'buynow')");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_registrations");

            migrationBuilder.DropIndex(
                name: "ix_auctions_listing_type",
                table: "auctions");

            migrationBuilder.DropCheckConstraint(
                name: "chk_auctions_listing_type",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "requires_registration",
                table: "auctions");
        }
    }
}
