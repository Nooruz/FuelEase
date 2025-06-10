using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelSaleNozzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales");

            migrationBuilder.AlterColumn<int>(
                name: "FuelNozzleId",
                table: "FuelSales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "NozzleId",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 26, 23, 20, 18, 645, DateTimeKind.Local).AddTicks(5267));

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_NozzleId",
                table: "FuelSales",
                column: "NozzleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales",
                column: "FuelNozzleId",
                principalTable: "FuelNozzles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales");

            migrationBuilder.DropIndex(
                name: "IX_FuelSales_NozzleId",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "NozzleId",
                table: "FuelSales");

            migrationBuilder.AlterColumn<int>(
                name: "FuelNozzleId",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 7, 16, 19, 45, 59, 146, DateTimeKind.Local).AddTicks(4711));

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                table: "FuelSales",
                column: "FuelNozzleId",
                principalTable: "FuelNozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
