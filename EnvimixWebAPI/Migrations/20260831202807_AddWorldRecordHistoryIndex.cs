using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldRecordHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Records_MapId_CarId_Gravity_Laps_Removed_DrivenAt_Time",
                table: "Records",
                columns: new[] { "MapId", "CarId", "Gravity", "Laps", "Removed", "DrivenAt", "Time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_MapId_CarId_Gravity_Laps_Removed_DrivenAt_Time",
                table: "Records");
        }
    }
}
