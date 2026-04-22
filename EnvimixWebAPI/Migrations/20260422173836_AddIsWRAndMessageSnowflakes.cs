using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsWRAndMessageSnowflakes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWorldRecord",
                table: "Records",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "RemovedMessageDiscordSnowflake",
                table: "Records",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "WorldRecordMessageDiscordSnowflake",
                table: "Records",
                type: "bigint unsigned",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWorldRecord",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "RemovedMessageDiscordSnowflake",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "WorldRecordMessageDiscordSnowflake",
                table: "Records");
        }
    }
}
