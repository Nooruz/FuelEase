using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelTransferColumnDeleteAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "FuelTransferColumns");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Nozzles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NozzleAddress",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 14, 18, 57, 53, 786, DateTimeKind.Local).AddTicks(2729));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "NozzleAddress",
                table: "Nozzles");

            migrationBuilder.AddColumn<int>(
                name: "Address",
                table: "FuelTransferColumns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 14, 18, 4, 51, 730, DateTimeKind.Local).AddTicks(2184));
        }
    }
}
