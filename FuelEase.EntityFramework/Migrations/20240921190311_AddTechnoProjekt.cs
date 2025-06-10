using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnoProjekt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EchoSuppression",
                table: "FuelTransferColumns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SurveyPeriod",
                table: "FuelTransferColumns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Timeout",
                table: "FuelTransferColumns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "USBPowerSupply",
                table: "FuelTransferColumns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 22, 1, 3, 10, 529, DateTimeKind.Local).AddTicks(563));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EchoSuppression",
                table: "FuelTransferColumns");

            migrationBuilder.DropColumn(
                name: "SurveyPeriod",
                table: "FuelTransferColumns");

            migrationBuilder.DropColumn(
                name: "Timeout",
                table: "FuelTransferColumns");

            migrationBuilder.DropColumn(
                name: "USBPowerSupply",
                table: "FuelTransferColumns");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 20, 22, 13, 1, 724, DateTimeKind.Local).AddTicks(1911));
        }
    }
}
