using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvimixWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCarOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE `Cars`
                SET `Order` = CASE `Id`
                    WHEN 'CanyonCar' THEN 0
                    WHEN 'StadiumCar' THEN 1
                    WHEN 'ValleyCar' THEN 2
                    WHEN 'LagoonCar' THEN 3
                    WHEN 'TrafficCar' THEN 4
                    WHEN 'DesertCar' THEN 5
                    WHEN 'SnowCar' THEN 6
                    WHEN 'RallyCar' THEN 7
                    WHEN 'IslandCar' THEN 8
                    WHEN 'BayCar' THEN 9
                    WHEN 'CoastCar' THEN 10
                    ELSE NULL
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Cars");
        }
    }
}
