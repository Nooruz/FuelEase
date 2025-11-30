using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFiscalDataIdInFuelSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_FiscalDatas_FiscalDataId",
                table: "FuelSales");

            migrationBuilder.DropIndex(
                name: "IX_FuelSales_FiscalDataId",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "FiscalDataId",
                table: "FuelSales");

            migrationBuilder.AddColumn<int>(
                name: "FuelSaleId",
                table: "FiscalDatas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 30, 19, 7, 37, 483, DateTimeKind.Local).AddTicks(1607));

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas",
                column: "FuelSaleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalDatas_FuelSales_FuelSaleId",
                table: "FiscalDatas",
                column: "FuelSaleId",
                principalTable: "FuelSales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalDatas_FuelSales_FuelSaleId",
                table: "FiscalDatas");

            migrationBuilder.DropIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "FuelSaleId",
                table: "FiscalDatas");

            migrationBuilder.AddColumn<int>(
                name: "FiscalDataId",
                table: "FuelSales",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 26, 18, 10, 46, 292, DateTimeKind.Local).AddTicks(5341));

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_FiscalDataId",
                table: "FuelSales",
                column: "FiscalDataId",
                unique: true,
                filter: "[FiscalDataId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_FiscalDatas_FiscalDataId",
                table: "FuelSales",
                column: "FiscalDataId",
                principalTable: "FiscalDatas",
                principalColumn: "Id");
        }
    }
}
