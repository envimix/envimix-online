using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMedals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthorTime",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BronzeTime",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GoldTime",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SilverTime",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorTime",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "BronzeTime",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "GoldTime",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "SilverTime",
                table: "Maps");
        }
    }
}
