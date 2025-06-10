using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFuelTransferColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Port",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaudRate = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Port", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelTransferColumn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Side = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    PortId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTransferColumn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelTransferColumn_Port_PortId",
                        column: x => x.PortId,
                        principalTable: "Port",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nozzle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tube = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    FuelTransferColumnId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nozzle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nozzle_FuelTransferColumn_FuelTransferColumnId",
                        column: x => x.FuelTransferColumnId,
                        principalTable: "FuelTransferColumn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nozzle_Tanks_TankId",
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
                value: new DateTime(2024, 7, 13, 16, 25, 26, 114, DateTimeKind.Local).AddTicks(2259));

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransferColumn_PortId",
                table: "FuelTransferColumn",
                column: "PortId");

            migrationBuilder.CreateIndex(
                name: "IX_Nozzle_FuelTransferColumnId",
                table: "Nozzle",
                column: "FuelTransferColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_Nozzle_TankId",
                table: "Nozzle",
                column: "TankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nozzle");

            migrationBuilder.DropTable(
                name: "FuelTransferColumn");

            migrationBuilder.DropTable(
                name: "Port");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 9, 22, 7, 35, 112, DateTimeKind.Local).AddTicks(6321));
        }
    }
}
