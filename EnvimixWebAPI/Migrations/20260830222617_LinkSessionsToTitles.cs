using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class LinkSessionsToTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EnvimaniaSessions_TitleId",
                table: "EnvimaniaSessions",
                column: "TitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnvimaniaSessions_Titles_TitleId",
                table: "EnvimaniaSessions",
                column: "TitleId",
                principalTable: "Titles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnvimaniaSessions_Titles_TitleId",
                table: "EnvimaniaSessions");

            migrationBuilder.DropIndex(
                name: "IX_EnvimaniaSessions_TitleId",
                table: "EnvimaniaSessions");
        }
    }
}
