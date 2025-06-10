using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelAddColorColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Fuels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                column: "Color",
                value: "#F6B511");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                column: "Color",
                value: "#ED2D38");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                column: "Color",
                value: "#4FA800");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                column: "Color",
                value: "#FFD700");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                column: "Color",
                value: "#737373");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 9, 20, 8, 8, 809, DateTimeKind.Local).AddTicks(2632));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Fuels");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 27, 22, 1, 5, 734, DateTimeKind.Local).AddTicks(4538));
        }
    }
}
