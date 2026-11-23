using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMapUnlockMedalPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnlockMedalPoints",
                table: "Maps",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnlockMedalPoints",
                table: "Maps");
        }
    }
}
