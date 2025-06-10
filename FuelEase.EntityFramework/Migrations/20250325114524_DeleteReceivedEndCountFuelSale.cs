using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DeleteReceivedEndCountFuelSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedBeginningCount",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "ReceivedEndCount",
                table: "FuelSales",
                newName: "ReceivedCount");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 25, 17, 45, 24, 812, DateTimeKind.Local).AddTicks(7868));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedCount",
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
                value: new DateTime(2025, 3, 25, 17, 41, 26, 966, DateTimeKind.Local).AddTicks(1355));
        }
    }
}
