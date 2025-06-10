using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class EditNozzleCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelNozzleCounters");

            migrationBuilder.CreateTable(
                name: "NozzleCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Beginning = table.Column<double>(type: "float", nullable: false),
                    Ending = table.Column<double>(type: "float", nullable: false),
                    FuelNozzleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NozzleCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NozzleCounters_FuelNozzles_FuelNozzleId",
                        column: x => x.FuelNozzleId,
                        principalTable: "FuelNozzles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NozzleCounters_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NozzleCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 26, 19, 8, 29, 907, DateTimeKind.Local).AddTicks(8832));

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_FuelNozzleId",
                table: "NozzleCounters",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_NozzleId",
                table: "NozzleCounters",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_ShiftId",
                table: "NozzleCounters",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NozzleCounters");

            migrationBuilder.CreateTable(
                name: "FuelNozzleCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuelNozzleId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Beginning = table.Column<double>(type: "float", nullable: false),
                    Ending = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelNozzleCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelNozzleCounters_FuelNozzles_FuelNozzleId",
                        column: x => x.FuelNozzleId,
                        principalTable: "FuelNozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelNozzleCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 22, 2, 1, 40, 689, DateTimeKind.Local).AddTicks(1147));

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzleCounters_FuelNozzleId",
                table: "FuelNozzleCounters",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzleCounters_ShiftId",
                table: "FuelNozzleCounters",
                column: "ShiftId");
        }
    }
}
