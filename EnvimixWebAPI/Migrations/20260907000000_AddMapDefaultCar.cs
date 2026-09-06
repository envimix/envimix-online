using EnvimixWebAPI;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260907000000_AddMapDefaultCar")]
    public partial class AddMapDefaultCar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCarId",
                table: "Maps",
                type: "varchar(16)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_DefaultCarId",
                table: "Maps",
                column: "DefaultCarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Cars_DefaultCarId",
                table: "Maps",
                column: "DefaultCarId",
                principalTable: "Cars",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Cars_DefaultCarId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_DefaultCarId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "DefaultCarId",
                table: "Maps");
        }
    }
}
