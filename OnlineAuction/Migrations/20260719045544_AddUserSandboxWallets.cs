using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSandboxWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_sandbox_wallets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sandbox_wallets", x => x.id);
                    table.CheckConstraint("chk_user_sandbox_wallets_balance", "balance >= 0");
                    table.ForeignKey(
                        name: "fk_user_sandbox_wallets_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_sandbox_wallets_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_sandbox_wallets_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_sandbox_wallets_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_sandbox_wallets_created_by",
                table: "user_sandbox_wallets",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_sandbox_wallets_deleted_at",
                table: "user_sandbox_wallets",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_sandbox_wallets_deleted_by",
                table: "user_sandbox_wallets",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_sandbox_wallets_updated_by",
                table: "user_sandbox_wallets",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_sandbox_wallets_user_id",
                table: "user_sandbox_wallets",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_sandbox_wallets");
        }
    }
}
