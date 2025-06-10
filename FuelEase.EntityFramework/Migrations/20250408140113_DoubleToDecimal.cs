using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DoubleToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "UnregisteredSales",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "EndCount",
                table: "TankShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "BeginCount",
                table: "TankShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Size",
                table: "Tanks",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumSize",
                table: "Tanks",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "EndCount",
                table: "NozzleShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "BeginCount",
                table: "NozzleShiftCounters",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceivedQuantity",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceivedCount",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalesTax",
                table: "Fuels",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "FuelIntakes",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumValue",
                table: "DiscountTariffPlans",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaximumValue",
                table: "DiscountTariffPlans",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountValue",
                table: "DiscountTariffPlans",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountQuantity",
                table: "DiscountSales",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                column: "SalesTax",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                column: "SalesTax",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                column: "SalesTax",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                column: "SalesTax",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                column: "SalesTax",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 8, 20, 1, 12, 915, DateTimeKind.Local).AddTicks(5210));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "UnregisteredSales",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "EndCount",
                table: "TankShiftCounters",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "BeginCount",
                table: "TankShiftCounters",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Size",
                table: "Tanks",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "MinimumSize",
                table: "Tanks",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "EndCount",
                table: "NozzleShiftCounters",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "BeginCount",
                table: "NozzleShiftCounters",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "ReceivedQuantity",
                table: "FuelSales",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "ReceivedCount",
                table: "FuelSales",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "FuelSales",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "SalesTax",
                table: "Fuels",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "FuelIntakes",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "MinimumValue",
                table: "DiscountTariffPlans",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "MaximumValue",
                table: "DiscountTariffPlans",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "DiscountValue",
                table: "DiscountTariffPlans",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "DiscountQuantity",
                table: "DiscountSales",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                column: "SalesTax",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                column: "SalesTax",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                column: "SalesTax",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                column: "SalesTax",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                column: "SalesTax",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 4, 1, 22, 19, 19, 907, DateTimeKind.Local).AddTicks(9624));
        }
    }
}
