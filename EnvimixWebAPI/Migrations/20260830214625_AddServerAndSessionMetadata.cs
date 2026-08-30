using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServerAndSessionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Servers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ServerModeName",
                table: "EnvimaniaSessions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TitleId",
                table: "EnvimaniaSessions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ServerModeName",
                table: "EnvimaniaSessions");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "EnvimaniaSessions");
        }
    }
}
