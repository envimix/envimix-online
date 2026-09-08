using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServerRegisteredBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegisteredById",
                table: "Servers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_RegisteredById",
                table: "Servers",
                column: "RegisteredById");

            migrationBuilder.AddForeignKey(
                name: "FK_Servers_Users_RegisteredById",
                table: "Servers",
                column: "RegisteredById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servers_Users_RegisteredById",
                table: "Servers");

            migrationBuilder.DropIndex(
                name: "IX_Servers_RegisteredById",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "RegisteredById",
                table: "Servers");
        }
    }
}
