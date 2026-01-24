using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMapData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataId",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MapData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapData", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_DataId",
                table: "Maps",
                column: "DataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_MapData_DataId",
                table: "Maps",
                column: "DataId",
                principalTable: "MapData",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_MapData_DataId",
                table: "Maps");

            migrationBuilder.DropTable(
                name: "MapData");

            migrationBuilder.DropIndex(
                name: "IX_Maps_DataId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "DataId",
                table: "Maps");
        }
    }
}
