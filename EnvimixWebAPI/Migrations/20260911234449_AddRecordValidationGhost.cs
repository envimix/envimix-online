using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordValidationGhost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ValidationGhostId",
                table: "Records",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Records_ValidationGhostId",
                table: "Records",
                column: "ValidationGhostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Ghosts_ValidationGhostId",
                table: "Records",
                column: "ValidationGhostId",
                principalTable: "Ghosts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Ghosts_ValidationGhostId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_ValidationGhostId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ValidationGhostId",
                table: "Records");
        }
    }
}
