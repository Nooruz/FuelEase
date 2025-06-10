using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleCountersToShiftCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EndCount",
                table: "NozzleShiftCounters",
                newName: "EndSaleCounter");

            migrationBuilder.RenameColumn(
                name: "BeginCount",
                table: "NozzleShiftCounters",
                newName: "EndNozzleCounter");

            migrationBuilder.AddColumn<decimal>(
                name: "BeginNozzleCounter",
                table: "NozzleShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BeginSaleCounter",
                table: "NozzleShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 21, 40, 27, 29, DateTimeKind.Local).AddTicks(6755));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeginNozzleCounter",
                table: "NozzleShiftCounters");

            migrationBuilder.DropColumn(
                name: "BeginSaleCounter",
                table: "NozzleShiftCounters");

            migrationBuilder.RenameColumn(
                name: "EndSaleCounter",
                table: "NozzleShiftCounters",
                newName: "EndCount");

            migrationBuilder.RenameColumn(
                name: "EndNozzleCounter",
                table: "NozzleShiftCounters",
                newName: "BeginCount");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 20, 1, 12, 915, DateTimeKind.Local).AddTicks(5210));
        }
    }
}
