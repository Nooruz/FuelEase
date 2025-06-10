using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftIdInFuelIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "FuelIntakes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 31, 22, 33, 3, 703, DateTimeKind.Local).AddTicks(4779));

            migrationBuilder.CreateIndex(
                name: "IX_FuelIntakes_ShiftId",
                table: "FuelIntakes",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelIntakes_Shifts_ShiftId",
                table: "FuelIntakes",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelIntakes_Shifts_ShiftId",
                table: "FuelIntakes");

            migrationBuilder.DropIndex(
                name: "IX_FuelIntakes_ShiftId",
                table: "FuelIntakes");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "FuelIntakes");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 30, 22, 48, 34, 759, DateTimeKind.Local).AddTicks(2242));
        }
    }
}
