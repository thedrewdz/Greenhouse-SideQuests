using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TempTest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddValveAndFanFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FanOn",
                table: "SensorData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ValveOn",
                table: "SensorData",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FanOn",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "ValveOn",
                table: "SensorData");
        }
    }
}
