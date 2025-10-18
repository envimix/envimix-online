using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixDiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConvertedMaps_OriginalUid",
                table: "ConvertedMaps");

            migrationBuilder.CreateIndex(
                name: "IX_ConvertedMaps_OriginalUid",
                table: "ConvertedMaps",
                column: "OriginalUid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConvertedMaps_OriginalUid",
                table: "ConvertedMaps");

            migrationBuilder.CreateIndex(
                name: "IX_ConvertedMaps_OriginalUid",
                table: "ConvertedMaps",
                column: "OriginalUid",
                unique: true);
        }
    }
}
