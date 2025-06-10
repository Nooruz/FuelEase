using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateTankShiftCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NozzleCounters");

            migrationBuilder.CreateTable(
                name: "NozzleShiftCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    BeginCount = table.Column<double>(type: "float", nullable: false),
                    EndCount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NozzleShiftCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NozzleShiftCounters_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NozzleShiftCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TankShiftCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    BeginCount = table.Column<double>(type: "float", nullable: false),
                    EndCount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TankShiftCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TankShiftCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TankShiftCounters_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 30, 22, 48, 34, 759, DateTimeKind.Local).AddTicks(2242));

            migrationBuilder.CreateIndex(
                name: "IX_NozzleShiftCounters_NozzleId",
                table: "NozzleShiftCounters",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_NozzleShiftCounters_ShiftId",
                table: "NozzleShiftCounters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_TankShiftCounters_ShiftId",
                table: "TankShiftCounters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_TankShiftCounters_TankId",
                table: "TankShiftCounters",
                column: "TankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NozzleShiftCounters");

            migrationBuilder.DropTable(
                name: "TankShiftCounters");

            migrationBuilder.CreateTable(
                name: "NozzleCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    BeginCount = table.Column<double>(type: "float", nullable: false),
                    EndCount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NozzleCounters", x => x.Id);
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
                value: new DateTime(2025, 3, 29, 15, 16, 14, 205, DateTimeKind.Local).AddTicks(3125));

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_NozzleId",
                table: "NozzleCounters",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_ShiftId",
                table: "NozzleCounters",
                column: "ShiftId");
        }
    }
}
