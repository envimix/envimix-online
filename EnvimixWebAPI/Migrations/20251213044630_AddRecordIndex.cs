using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_Gravity",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_Laps",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_MapId",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_MapId_UserId_CarId_Gravity_Laps_Time_DrivenAt",
                table: "Records",
                columns: new[] { "MapId", "UserId", "CarId", "Gravity", "Laps", "Time", "DrivenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_MapId_UserId_CarId_Gravity_Laps_Time_DrivenAt",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_Gravity",
                table: "Records",
                column: "Gravity");

            migrationBuilder.CreateIndex(
                name: "IX_Records_Laps",
                table: "Records",
                column: "Laps");

            migrationBuilder.CreateIndex(
                name: "IX_Records_MapId",
                table: "Records",
                column: "MapId");
        }
    }
}
