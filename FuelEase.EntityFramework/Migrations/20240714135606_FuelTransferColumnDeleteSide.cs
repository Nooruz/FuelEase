using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelTransferColumnDeleteSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Side",
                table: "FuelTransferColumns");

            migrationBuilder.AddColumn<int>(
                name: "Side",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 14, 19, 56, 6, 16, DateTimeKind.Local).AddTicks(2109));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Side",
                table: "Nozzles");

            migrationBuilder.AddColumn<int>(
                name: "Side",
                table: "FuelTransferColumns",
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
    }
}
