using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnoProjektNozzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutorunType",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ControlCounter",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ControlRequest",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CounterType",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CounterValue",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DisableControlFilling",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "InvertStartStop",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MechanicalCounters",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequestCounter",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequestFromFuelTransferColumn",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartDelay",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartStopButton",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UNBKAddress",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 22, 2, 1, 40, 689, DateTimeKind.Local).AddTicks(1147));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutorunType",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ControlCounter",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ControlRequest",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "CounterType",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "CounterValue",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "DisableControlFilling",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "InvertStartStop",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "MechanicalCounters",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "RequestCounter",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "RequestFromFuelTransferColumn",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "StartDelay",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "StartStopButton",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "UNBKAddress",
                table: "Nozzles");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 22, 1, 3, 10, 529, DateTimeKind.Local).AddTicks(563));
        }
    }
}
