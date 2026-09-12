using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionReplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplayId",
                table: "EnvimaniaSessions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_EnvimaniaSessions_ReplayId",
                table: "EnvimaniaSessions",
                column: "ReplayId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnvimaniaSessions_Replays_ReplayId",
                table: "EnvimaniaSessions",
                column: "ReplayId",
                principalTable: "Replays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnvimaniaSessions_Replays_ReplayId",
                table: "EnvimaniaSessions");

            migrationBuilder.DropIndex(
                name: "IX_EnvimaniaSessions_ReplayId",
                table: "EnvimaniaSessions");

            migrationBuilder.DropColumn(
                name: "ReplayId",
                table: "EnvimaniaSessions");
        }
    }
}
