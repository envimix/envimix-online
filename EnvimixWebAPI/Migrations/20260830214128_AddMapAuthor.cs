using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMapAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorLogin",
                table: "Maps",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AuthorNickname",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorLogin",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "AuthorNickname",
                table: "Maps");
        }
    }
}
