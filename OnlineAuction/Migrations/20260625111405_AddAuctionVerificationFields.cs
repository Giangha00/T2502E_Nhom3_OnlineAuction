using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reject_reason",
                table: "auctions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verified_by",
                table: "auctions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_auctions_verified_by",
                table: "auctions",
                column: "verified_by");

            migrationBuilder.AddCheckConstraint(
                name: "chk_auctions_status",
                table: "auctions",
                sql: "`status` IN ('pending_review','rejected','scheduled','live','ending_soon','ended','awaiting_payment','completed','cancelled')");

            migrationBuilder.AddForeignKey(
                name: "fk_auctions_verified_by",
                table: "auctions",
                column: "verified_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_auctions_verified_by",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "IX_auctions_verified_by",
                table: "auctions");

            migrationBuilder.DropCheckConstraint(
                name: "chk_auctions_status",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "verified_by",
                table: "auctions");
        }
    }
}
