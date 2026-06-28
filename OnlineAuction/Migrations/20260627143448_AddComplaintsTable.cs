using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_reference = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    order_id = table.Column<int>(type: "int", nullable: true),
                    order_reference = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    complaint_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "refund")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    contact_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contact_email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "pending")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admin_notes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolution_note = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    evidence_urls = table.Column<string>(type: "text", nullable: true)
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
                    table.PrimaryKey("PK_complaints", x => x.id);
                    table.CheckConstraint("chk_complaints_status", "`status` IN ('pending','under_review','approved','rejected','closed')");
                    table.CheckConstraint("chk_complaints_type", "`complaint_type` IN ('refund','dispute','authenticity','other')");
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_complaints_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "complaints");
        }
    }
}
