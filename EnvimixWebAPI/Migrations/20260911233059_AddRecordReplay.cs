using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordReplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplayId",
                table: "Records",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Records_ReplayId",
                table: "Records",
                column: "ReplayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Replays_ReplayId",
                table: "Records",
                column: "ReplayId",
                principalTable: "Replays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Replays_ReplayId",
                table: "Records");

            migrationBuilder.DropTable(
                name: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Records_ReplayId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ReplayId",
                table: "Records");
        }
    }
}
