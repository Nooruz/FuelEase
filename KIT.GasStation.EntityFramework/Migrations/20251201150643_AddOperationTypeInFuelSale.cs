using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationTypeInFuelSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountSaleId",
                table: "FuelSales");

            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 12, 1, 21, 6, 42, 411, DateTimeKind.Local).AddTicks(7085));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "FuelSales");

            migrationBuilder.AddColumn<int>(
                name: "DiscountSaleId",
                table: "FuelSales",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 30, 19, 7, 37, 483, DateTimeKind.Local).AddTicks(1607));
        }
    }
}
