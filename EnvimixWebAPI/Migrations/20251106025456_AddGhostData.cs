using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGhostData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "GhostData",
                table: "Records",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ServersideDrivenAt",
                table: "Records",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhostData",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ServersideDrivenAt",
                table: "Records");
        }
    }
}
