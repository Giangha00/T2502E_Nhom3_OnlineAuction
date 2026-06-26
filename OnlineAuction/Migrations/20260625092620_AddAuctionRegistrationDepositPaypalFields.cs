using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionRegistrationDepositPaypalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auction_registration_deposits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    auction_registration_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paypal_order_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paypal_capture_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paypal_refund_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_registration_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_auction_registration_deposits_auction_registrations_auction_~",
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_auction_registration_deposits_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_auction_registration_deposits_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_registration_deposits");
        }
    }
}
