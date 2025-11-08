using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGhostEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhostData",
                table: "Records");

            migrationBuilder.AddColumn<Guid>(
                name: "GhostId",
                table: "Records",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Ghosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ghosts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Records_GhostId",
                table: "Records",
                column: "GhostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Ghosts_GhostId",
                table: "Records",
                column: "GhostId",
                principalTable: "Ghosts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Ghosts_GhostId",
                table: "Records");

            migrationBuilder.DropTable(
                name: "Ghosts");

            migrationBuilder.DropIndex(
                name: "IX_Records_GhostId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "GhostId",
                table: "Records");

            migrationBuilder.AddColumn<byte[]>(
                name: "GhostData",
                table: "Records",
                type: "longblob",
                nullable: true);
        }
    }
}
