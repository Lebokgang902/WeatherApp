using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Migrations
{
    /// <inheritdoc />
    public partial class SetNullOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots");

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots");

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }
    }
}
