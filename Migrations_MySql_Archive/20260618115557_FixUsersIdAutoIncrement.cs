using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class FixUsersIdAutoIncrement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @has_auto_increment := (
                    SELECT COUNT(*)
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'users'
                      AND COLUMN_NAME = 'id'
                      AND EXTRA LIKE '%auto_increment%'
                );

                SET @sql := IF(
                    @has_auto_increment = 0,
                    'ALTER TABLE `users` MODIFY COLUMN `id` int NOT NULL AUTO_INCREMENT',
                    'SELECT 1');

                SET FOREIGN_KEY_CHECKS = 0;
                PREPARE fix_users_id_stmt FROM @sql;
                EXECUTE fix_users_id_stmt;
                DEALLOCATE PREPARE fix_users_id_stmt;
                SET FOREIGN_KEY_CHECKS = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
