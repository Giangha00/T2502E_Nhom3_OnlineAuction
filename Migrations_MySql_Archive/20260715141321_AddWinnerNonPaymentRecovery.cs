using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddWinnerNonPaymentRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "forfeited_at",
                table: "auction_registration_deposits",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "winner_non_payment_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    cancelled_order_id = table.Column<int>(type: "int", nullable: false),
                    defaulting_user_id = table.Column<int>(type: "int", nullable: false),
                    forfeited_deposit_id = table.Column<long>(type: "bigint", nullable: true),
                    forfeited_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    second_chance_user_id = table.Column<int>(type: "int", nullable: true),
                    second_chance_order_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_winner_non_payment_logs_auction_id",
                table: "winner_non_payment_logs",
                column: "auction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "winner_non_payment_logs");

            migrationBuilder.DropColumn(
                name: "forfeited_at",
                table: "auction_registration_deposits");
        }
    }
}
