using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFuelNozzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales");

            migrationBuilder.DropForeignKey(
                name: "FK_NozzleCounters_FuelNozzles_FuelNozzleId",
                table: "NozzleCounters");

            migrationBuilder.DropForeignKey(
                name: "FK_UnregisteredSales_FuelNozzles_FuelNozzleId",
                table: "UnregisteredSales");

            migrationBuilder.DropTable(
                name: "FuelNozzles");

            migrationBuilder.DropTable(
                name: "Controllers");

            migrationBuilder.DropIndex(
                name: "IX_NozzleCounters_FuelNozzleId",
                table: "NozzleCounters");

            migrationBuilder.DropIndex(
                name: "IX_FuelSales_FuelNozzleId",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "FuelNozzleId",
                table: "NozzleCounters");

            migrationBuilder.DropColumn(
                name: "FuelNozzleId",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "FuelNozzleId",
                table: "UnregisteredSales",
                newName: "NozzleId");

            migrationBuilder.RenameIndex(
                name: "IX_UnregisteredSales_FuelNozzleId",
                table: "UnregisteredSales",
                newName: "IX_UnregisteredSales_NozzleId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 28, 23, 2, 2, 932, DateTimeKind.Local).AddTicks(807));

            migrationBuilder.AddForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales");

            migrationBuilder.RenameColumn(
                name: "NozzleId",
                table: "UnregisteredSales",
                newName: "FuelNozzleId");

            migrationBuilder.RenameIndex(
                name: "IX_UnregisteredSales_NozzleId",
                table: "UnregisteredSales",
                newName: "IX_UnregisteredSales_FuelNozzleId");

            migrationBuilder.AddColumn<int>(
                name: "FuelNozzleId",
                table: "NozzleCounters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FuelNozzleId",
                table: "FuelSales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Controllers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    ControllerType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controllers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelNozzles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControllerId = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nozzle = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    Tube = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelNozzles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelNozzles_Controllers_ControllerId",
                        column: x => x.ControllerId,
                        principalTable: "Controllers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelNozzles_Tanks_TankId",
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
                value: new DateTime(2024, 9, 28, 22, 49, 3, 360, DateTimeKind.Local).AddTicks(7156));

            migrationBuilder.CreateIndex(
                name: "IX_NozzleCounters_FuelNozzleId",
                table: "NozzleCounters",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_FuelNozzleId",
                table: "FuelSales",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzles_ControllerId",
                table: "FuelNozzles",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzles_TankId",
                table: "FuelNozzles",
                column: "TankId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales",
                column: "FuelNozzleId",
                principalTable: "FuelNozzles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NozzleCounters_FuelNozzles_FuelNozzleId",
                table: "NozzleCounters",
                column: "FuelNozzleId",
                principalTable: "FuelNozzles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnregisteredSales_FuelNozzles_FuelNozzleId",
                table: "UnregisteredSales",
                column: "FuelNozzleId",
                principalTable: "FuelNozzles",
                principalColumn: "Id");
        }
    }
}
