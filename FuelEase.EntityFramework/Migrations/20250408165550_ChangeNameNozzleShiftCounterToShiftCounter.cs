using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameNozzleShiftCounterToShiftCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NozzleShiftCounters_Nozzles_NozzleId",
                table: "NozzleShiftCounters");

            migrationBuilder.DropForeignKey(
                name: "FK_NozzleShiftCounters_Shifts_ShiftId",
                table: "NozzleShiftCounters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NozzleShiftCounters",
                table: "NozzleShiftCounters");

            migrationBuilder.RenameTable(
                name: "NozzleShiftCounters",
                newName: "ShiftCounters");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleShiftCounters_ShiftId",
                table: "ShiftCounters",
                newName: "IX_ShiftCounters_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleShiftCounters_NozzleId",
                table: "ShiftCounters",
                newName: "IX_ShiftCounters_NozzleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShiftCounters",
                table: "ShiftCounters",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 22, 55, 49, 914, DateTimeKind.Local).AddTicks(4903));

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftCounters_Nozzles_NozzleId",
                table: "ShiftCounters",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftCounters_Shifts_ShiftId",
                table: "ShiftCounters",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftCounters_Nozzles_NozzleId",
                table: "ShiftCounters");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftCounters_Shifts_ShiftId",
                table: "ShiftCounters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShiftCounters",
                table: "ShiftCounters");

            migrationBuilder.RenameTable(
                name: "ShiftCounters",
                newName: "NozzleShiftCounters");

            migrationBuilder.RenameIndex(
                name: "IX_ShiftCounters_ShiftId",
                table: "NozzleShiftCounters",
                newName: "IX_NozzleShiftCounters_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_ShiftCounters_NozzleId",
                table: "NozzleShiftCounters",
                newName: "IX_NozzleShiftCounters_NozzleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NozzleShiftCounters",
                table: "NozzleShiftCounters",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 21, 40, 27, 29, DateTimeKind.Local).AddTicks(6755));

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleShiftCounters_Nozzles_NozzleId",
                table: "NozzleShiftCounters",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleShiftCounters_Shifts_ShiftId",
                table: "NozzleShiftCounters",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
