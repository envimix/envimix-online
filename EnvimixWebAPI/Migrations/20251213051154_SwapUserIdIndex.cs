using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class SwapUserIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_MapId_CarId_Gravity_Laps_UserId_Time_DrivenAt",
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
                name: "IX_Records_MapId_CarId_Gravity_Laps_UserId_Time_DrivenAt",
                table: "Records",
                columns: new[] { "MapId", "CarId", "Gravity", "Laps", "UserId", "Time", "DrivenAt" });
        }
    }
}
