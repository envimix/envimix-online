using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIpsRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "DataBackup",
                table: "Replays",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IpsRemoved",
                table: "Replays",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "DataBackup",
                table: "Ghosts",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IpsRemoved",
                table: "Ghosts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataBackup",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "IpsRemoved",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "DataBackup",
                table: "Ghosts");

            migrationBuilder.DropColumn(
                name: "IpsRemoved",
                table: "Ghosts");
        }
    }
}
