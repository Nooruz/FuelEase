using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFiscalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas");

            migrationBuilder.DeleteData(
                table: "UnitOfMeasurements",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "CustomerSum",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "ReturnCheck",
                table: "FiscalDatas",
                newName: "Tnved");

            migrationBuilder.AddColumn<string>(
                name: "FuelName",
                table: "FiscalDatas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "FiscalDatas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "FiscalDatas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "FiscalDatas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "FiscalDatas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesTax",
                table: "FiscalDatas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SourceFiscalData",
                table: "FiscalDatas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "FiscalDatas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasurement",
                table: "FiscalDatas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ValueAddedTax",
                table: "FiscalDatas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 2, 17, 16, 26, 46, 986, DateTimeKind.Local).AddTicks(9610));

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas",
                column: "FuelSaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "FuelName",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "SalesTax",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "SourceFiscalData",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasurement",
                table: "FiscalDatas");

            migrationBuilder.DropColumn(
                name: "ValueAddedTax",
                table: "FiscalDatas");

            migrationBuilder.RenameColumn(
                name: "Tnved",
                table: "FiscalDatas",
                newName: "ReturnCheck");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "UnitOfMeasurements",
                columns: new[] { "Id", "Name" },
                values: new object[] { 3, "кВт*ч" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 12, 29, 21, 10, 38, 284, DateTimeKind.Local).AddTicks(6231));

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDatas_FuelSaleId",
                table: "FiscalDatas",
                column: "FuelSaleId",
                unique: true);
        }
    }
}
