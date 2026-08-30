using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class MakeSessionEndedAtNullable : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
