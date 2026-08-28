using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TempTest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprayAndFanEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FanEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTemperature = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    EndTemperature = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    StartHumidity = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EndHumidity = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FanEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SprayEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTemperature = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    EndTemperature = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    StartHumidity = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EndHumidity = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WaterUsedMilliliters = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprayEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FanEvents");

            migrationBuilder.DropTable(
                name: "SprayEvents");
        }
    }
}
