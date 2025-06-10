using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddPKElectronicsSettungs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Blocked",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlockingChannelOperation",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConstantFlowReduction",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NozzleSensor",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverflowConstant",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PumpAccelerationTime",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SurveyMethod",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 22, 50, 43, 266, DateTimeKind.Local).AddTicks(6055));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blocked",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "BlockingChannelOperation",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ConstantFlowReduction",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "NozzleSensor",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "OverflowConstant",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "PumpAccelerationTime",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "SurveyMethod",
                table: "Nozzles");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 26, 23, 20, 18, 645, DateTimeKind.Local).AddTicks(5267));
        }
    }
}
