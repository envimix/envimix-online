using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordValidationReplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ValidationReplayId",
                table: "Records",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Records_ValidationReplayId",
                table: "Records",
                column: "ValidationReplayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Replays_ValidationReplayId",
                table: "Records",
                column: "ValidationReplayId",
                principalTable: "Replays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Replays_ValidationReplayId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_ValidationReplayId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ValidationReplayId",
                table: "Records");
        }
    }
}
