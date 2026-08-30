using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServerRegisteredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegisteredAt",
                table: "Servers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE `Servers` AS `server`
                LEFT JOIN (
                    SELECT `ServerId`, MIN(`StartedAt`) AS `RegisteredAt`
                    FROM `EnvimaniaSessions`
                    GROUP BY `ServerId`
                ) AS `firstSession` ON `firstSession`.`ServerId` = `server`.`Id`
                SET `server`.`RegisteredAt` = COALESCE(`firstSession`.`RegisteredAt`, UTC_TIMESTAMP(6));
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RegisteredAt",
                table: "Servers",
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
                name: "RegisteredAt",
                table: "Servers");
        }
    }
}
