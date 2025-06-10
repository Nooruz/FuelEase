using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelNozzleAddColumnTube : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NozzleArmType",
                table: "FuelNozzles",
                newName: "Tube");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 9, 22, 7, 35, 112, DateTimeKind.Local).AddTicks(6321));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tube",
                table: "FuelNozzles",
                newName: "NozzleArmType");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 3, 9, 19, 21, 59, 9, DateTimeKind.Local).AddTicks(6872));
        }
    }
}
