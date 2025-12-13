using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class SwapUserIdIndex2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_MapId_UserId_CarId_Gravity_Laps_Time_DrivenAt",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_UserId_MapId_CarId_Gravity_Laps_Time_DrivenAt",
                table: "Records",
                columns: new[] { "UserId", "MapId", "CarId", "Gravity", "Laps", "Time", "DrivenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_UserId_MapId_CarId_Gravity_Laps_Time_DrivenAt",
                table: "Records");
        }
    }
}
