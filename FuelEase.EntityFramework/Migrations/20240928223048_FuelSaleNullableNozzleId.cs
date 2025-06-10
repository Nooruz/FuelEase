using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelSaleNullableNozzleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales");

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
                table: "FuelSales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 29, 4, 30, 47, 743, DateTimeKind.Local).AddTicks(6077));

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales");

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
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
                value: new DateTime(2024, 9, 28, 23, 2, 2, 932, DateTimeKind.Local).AddTicks(807));

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
