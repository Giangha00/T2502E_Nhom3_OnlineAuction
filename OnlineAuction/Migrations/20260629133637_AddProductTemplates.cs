using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "product_template_id",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    set_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    card_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    grade_label = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    year = table.Column<int>(type: "int", nullable: true),
                    language = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    short_description = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_html = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    primary_image = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_product_templates_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_product_templates_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_template_id",
                table: "products",
                column: "product_template_id");

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

            // Data migration: create one template per existing product group and link active products.
            // Grouping key: category_id, normalized name, set_name, card_number, grade_label.
            migrationBuilder.Sql(
                """
                INSERT INTO product_templates
                    (name, category_id, set_name, card_number, grade_label, year, language,
                     short_description, description_html, primary_image, is_active, created_at)
                SELECT
                    p.name,
                    p.category_id,
                    p.set_name,
                    p.card_number,
                    p.grade_label,
                    p.year,
                    p.language,
                    p.short_description,
                    p.description_html,
                    p.primary_image,
                    TRUE,
                    UTC_TIMESTAMP(6)
                FROM products p
                INNER JOIN (
                    SELECT MIN(id) AS product_id
                    FROM products
                    WHERE deleted_at IS NULL
                    GROUP BY
                        category_id,
                        UPPER(TRIM(name)) COLLATE utf8mb4_unicode_ci,
                        UPPER(TRIM(COALESCE(set_name, ''))) COLLATE utf8mb4_unicode_ci,
                        UPPER(TRIM(COALESCE(card_number, ''))) COLLATE utf8mb4_unicode_ci,
                        UPPER(TRIM(COALESCE(grade_label, ''))) COLLATE utf8mb4_unicode_ci
                ) grouped_products ON grouped_products.product_id = p.id
                WHERE p.deleted_at IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE products p
                INNER JOIN product_templates pt
                    ON pt.deleted_at IS NULL
                    AND pt.category_id = p.category_id
                    AND UPPER(TRIM(pt.name)) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(p.name)) COLLATE utf8mb4_unicode_ci
                    AND UPPER(TRIM(COALESCE(pt.set_name, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.set_name, ''))) COLLATE utf8mb4_unicode_ci
                    AND UPPER(TRIM(COALESCE(pt.card_number, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.card_number, ''))) COLLATE utf8mb4_unicode_ci
                    AND UPPER(TRIM(COALESCE(pt.grade_label, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.grade_label, ''))) COLLATE utf8mb4_unicode_ci
                SET p.product_template_id = pt.id
                WHERE p.deleted_at IS NULL
                    AND p.product_template_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_template",
                table: "products",
                column: "product_template_id",
                principalTable: "product_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_product_template",
                table: "products");

            migrationBuilder.DropTable(
                name: "product_templates");

            migrationBuilder.DropIndex(
                name: "ix_products_product_template_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_template_id",
                table: "products");
        }
    }
}
