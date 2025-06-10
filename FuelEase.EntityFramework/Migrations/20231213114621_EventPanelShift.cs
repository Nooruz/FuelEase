using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class EventPanelShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "EventsPanel",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2023, 12, 13, 17, 46, 21, 767, DateTimeKind.Local).AddTicks(322));

            migrationBuilder.CreateIndex(
                name: "IX_EventsPanel_ShiftId",
                table: "EventsPanel",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventsPanel_Shifts_ShiftId",
                table: "EventsPanel",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventsPanel_Shifts_ShiftId",
                table: "EventsPanel");

            migrationBuilder.DropIndex(
                name: "IX_EventsPanel_ShiftId",
                table: "EventsPanel");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "EventsPanel");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2023, 12, 13, 17, 41, 13, 709, DateTimeKind.Local).AddTicks(1048));
        }
    }
}
