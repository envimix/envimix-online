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
            migrationBuilder.CreateIndex(
                name: "IX_Records_MapId_CarId_Gravity_Laps_UserId_Time_DrivenAt",
                table: "Records",
                columns: new[] { "MapId", "CarId", "Gravity", "Laps", "UserId", "Time", "DrivenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_MapId_CarId_Gravity_Laps_UserId_Time_DrivenAt",
                table: "Records");
        }
    }
}
