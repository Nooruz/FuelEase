using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class NozzleCounterToNozzleCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NozzleCounter_Nozzles_NozzleId",
                table: "NozzleCounter");

            migrationBuilder.DropForeignKey(
                name: "FK_NozzleCounter_Shifts_ShiftId",
                table: "NozzleCounter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NozzleCounter",
                table: "NozzleCounter");

            migrationBuilder.RenameTable(
                name: "NozzleCounter",
                newName: "NozzleCounters");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleCounter_ShiftId",
                table: "NozzleCounters",
                newName: "IX_NozzleCounters_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleCounter_NozzleId",
                table: "NozzleCounters",
                newName: "IX_NozzleCounters_NozzleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NozzleCounters",
                table: "NozzleCounters",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 25, 22, 3, 36, 154, DateTimeKind.Local).AddTicks(1156));

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleCounters_Nozzles_NozzleId",
                table: "NozzleCounters",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleCounters_Shifts_ShiftId",
                table: "NozzleCounters",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NozzleCounters_Nozzles_NozzleId",
                table: "NozzleCounters");

            migrationBuilder.DropForeignKey(
                name: "FK_NozzleCounters_Shifts_ShiftId",
                table: "NozzleCounters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NozzleCounters",
                table: "NozzleCounters");

            migrationBuilder.RenameTable(
                name: "NozzleCounters",
                newName: "NozzleCounter");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleCounters_ShiftId",
                table: "NozzleCounter",
                newName: "IX_NozzleCounter_ShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_NozzleCounters_NozzleId",
                table: "NozzleCounter",
                newName: "IX_NozzleCounter_NozzleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NozzleCounter",
                table: "NozzleCounter",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 25, 17, 45, 24, 812, DateTimeKind.Local).AddTicks(7868));

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleCounter_Nozzles_NozzleId",
                table: "NozzleCounter",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleCounter_Shifts_ShiftId",
                table: "NozzleCounter",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
