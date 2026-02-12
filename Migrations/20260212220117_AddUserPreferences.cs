using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemperatureUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindSpeedUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshInterval = table.Column<int>(type: "int", nullable: false),
                    AutoRefreshEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowCoordinates = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_Id",
                table: "UserPreferences",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
