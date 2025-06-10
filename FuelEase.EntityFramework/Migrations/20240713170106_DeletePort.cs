using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DeletePort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelTransferColumn_Port_PortId",
                table: "FuelTransferColumn");

            migrationBuilder.DropForeignKey(
                name: "FK_Nozzle_FuelTransferColumn_FuelTransferColumnId",
                table: "Nozzle");

            migrationBuilder.DropForeignKey(
                name: "FK_Nozzle_Tanks_TankId",
                table: "Nozzle");

            migrationBuilder.DropTable(
                name: "Port");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Nozzle",
                table: "Nozzle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FuelTransferColumn",
                table: "FuelTransferColumn");

            migrationBuilder.DropIndex(
                name: "IX_FuelTransferColumn_PortId",
                table: "FuelTransferColumn");

            migrationBuilder.RenameTable(
                name: "Nozzle",
                newName: "Nozzles");

            migrationBuilder.RenameTable(
                name: "FuelTransferColumn",
                newName: "FuelTransferColumns");

            migrationBuilder.RenameIndex(
                name: "IX_Nozzle_TankId",
                table: "Nozzles",
                newName: "IX_Nozzles_TankId");

            migrationBuilder.RenameIndex(
                name: "IX_Nozzle_FuelTransferColumnId",
                table: "Nozzles",
                newName: "IX_Nozzles_FuelTransferColumnId");

            migrationBuilder.RenameColumn(
                name: "PortId",
                table: "FuelTransferColumns",
                newName: "BaudRate");

            migrationBuilder.AddColumn<string>(
                name: "PortName",
                table: "FuelTransferColumns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Nozzles",
                table: "Nozzles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FuelTransferColumns",
                table: "FuelTransferColumns",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 13, 23, 1, 6, 529, DateTimeKind.Local).AddTicks(3381));

            migrationBuilder.AddForeignKey(
                name: "FK_Nozzles_FuelTransferColumns_FuelTransferColumnId",
                table: "Nozzles",
                column: "FuelTransferColumnId",
                principalTable: "FuelTransferColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nozzles_Tanks_TankId",
                table: "Nozzles",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nozzles_FuelTransferColumns_FuelTransferColumnId",
                table: "Nozzles");

            migrationBuilder.DropForeignKey(
                name: "FK_Nozzles_Tanks_TankId",
                table: "Nozzles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Nozzles",
                table: "Nozzles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FuelTransferColumns",
                table: "FuelTransferColumns");

            migrationBuilder.DropColumn(
                name: "PortName",
                table: "FuelTransferColumns");

            migrationBuilder.RenameTable(
                name: "Nozzles",
                newName: "Nozzle");

            migrationBuilder.RenameTable(
                name: "FuelTransferColumns",
                newName: "FuelTransferColumn");

            migrationBuilder.RenameIndex(
                name: "IX_Nozzles_TankId",
                table: "Nozzle",
                newName: "IX_Nozzle_TankId");

            migrationBuilder.RenameIndex(
                name: "IX_Nozzles_FuelTransferColumnId",
                table: "Nozzle",
                newName: "IX_Nozzle_FuelTransferColumnId");

            migrationBuilder.RenameColumn(
                name: "BaudRate",
                table: "FuelTransferColumn",
                newName: "PortId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Nozzle",
                table: "Nozzle",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FuelTransferColumn",
                table: "FuelTransferColumn",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Port",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Port", x => x.Id);
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

            migrationBuilder.AddForeignKey(
                name: "FK_FuelTransferColumn_Port_PortId",
                table: "FuelTransferColumn",
                column: "PortId",
                principalTable: "Port",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nozzle_FuelTransferColumn_FuelTransferColumnId",
                table: "Nozzle",
                column: "FuelTransferColumnId",
                principalTable: "FuelTransferColumn",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nozzle_Tanks_TankId",
                table: "Nozzle",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
