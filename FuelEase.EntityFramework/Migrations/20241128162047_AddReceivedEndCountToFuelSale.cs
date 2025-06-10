using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedEndCountToFuelSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedCountQuantity",
                table: "FuelSales",
                newName: "ReceivedEndCount");

            migrationBuilder.AddColumn<double>(
                name: "ReceivedBeginningCount",
                table: "FuelSales",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 28, 22, 20, 46, 890, DateTimeKind.Local).AddTicks(620));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedBeginningCount",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "ReceivedEndCount",
                table: "FuelSales",
                newName: "ReceivedCountQuantity");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 28, 22, 9, 46, 703, DateTimeKind.Local).AddTicks(3932));
        }
    }
}
