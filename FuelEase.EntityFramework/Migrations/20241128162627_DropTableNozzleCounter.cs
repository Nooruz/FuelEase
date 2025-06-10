using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DropTableNozzleCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NozzleCounters");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 28, 22, 26, 26, 788, DateTimeKind.Local).AddTicks(7744));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NozzleCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Beginning = table.Column<double>(type: "float", nullable: false),
                    Ending = table.Column<double>(type: "float", nullable: false)
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
                value: new DateTime(2024, 11, 28, 22, 20, 46, 890, DateTimeKind.Local).AddTicks(620));

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
