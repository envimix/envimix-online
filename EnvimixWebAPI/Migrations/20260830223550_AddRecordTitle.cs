using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TitleId",
                table: "Records",
                type: "varchar(64)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE `Records` AS `record`
                INNER JOIN `Maps` AS `map` ON `map`.`Id` = `record`.`MapId`
                SET `record`.`TitleId` = `map`.`TitlePackId`
                WHERE `map`.`TitlePackId` IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Records_TitleId",
                table: "Records",
                column: "TitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Titles_TitleId",
                table: "Records",
                column: "TitleId",
                principalTable: "Titles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Titles_TitleId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_TitleId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "Records");
        }
    }
}
