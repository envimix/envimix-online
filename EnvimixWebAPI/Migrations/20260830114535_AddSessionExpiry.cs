using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "EnvimaniaSessions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE `EnvimaniaSessions`
                SET `ExpiresAt` = DATE_ADD(`StartedAt`, INTERVAL 20 MINUTE)
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "EnvimaniaSessions",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime(6)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "EnvimaniaSessions");
        }
    }
}
