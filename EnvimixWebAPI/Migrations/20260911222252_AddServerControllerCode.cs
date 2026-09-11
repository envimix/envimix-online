using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServerControllerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ControllerCodeHash",
                table: "Servers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ControllerCodeSalt",
                table: "Servers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ControllerCodeHash",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ControllerCodeSalt",
                table: "Servers");
        }
    }
}
