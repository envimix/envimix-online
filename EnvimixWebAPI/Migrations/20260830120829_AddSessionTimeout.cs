using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTimeout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "EnvimaniaSessions",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime(6)");

            migrationBuilder.Sql("""
                UPDATE `EnvimaniaSessions`
                SET `EndedAt` = NULL
                WHERE `FinishedGracefully` = FALSE
                """);

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
            migrationBuilder.Sql("""
                UPDATE `EnvimaniaSessions`
                SET `EndedAt` = `ExpiresAt`
                WHERE `EndedAt` IS NULL
                """);

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "EnvimaniaSessions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "EnvimaniaSessions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
