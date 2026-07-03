using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddBidFraudDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "flag_reason",
                table: "bids",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "bids",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_flagged",
                table: "bids",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "bids",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bid_fraud_alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    bid_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    alert_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metadata_json = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "open")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bid_fraud_alerts", x => x.id);
                    table.CheckConstraint("chk_fraud_alerts_severity", "`severity` IN ('low','medium','high')");
                    table.CheckConstraint("chk_fraud_alerts_status", "`status` IN ('open','reviewed','dismissed')");
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fraud_alerts_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fraud_alerts_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_ip_address",
                table: "bids",
                columns: new[] { "auction_id", "ip_address" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bid_fraud_alerts");

            migrationBuilder.DropIndex(
                name: "ix_bids_auction_ip_address",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "flag_reason",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "is_flagged",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "bids");
        }
    }
}
