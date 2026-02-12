using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Locations_City_Country",
                table: "Locations");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "WeatherSnapshots",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Locations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_City_Country",
                table: "Locations",
                columns: new[] { "City", "Country" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Locations_City_Country",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Locations");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "WeatherSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_City_Country",
                table: "Locations",
                columns: new[] { "City", "Country" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherSnapshots_Locations_LocationId",
                table: "WeatherSnapshots",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
