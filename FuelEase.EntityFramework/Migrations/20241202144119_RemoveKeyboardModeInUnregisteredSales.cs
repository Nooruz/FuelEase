using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKeyboardModeInUnregisteredSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales");

            migrationBuilder.DropForeignKey(
                name: "FK_UnregisteredSales_Tanks_TankId",
                table: "UnregisteredSales");

            migrationBuilder.DropIndex(
                name: "IX_UnregisteredSales_TankId",
                table: "UnregisteredSales");

            migrationBuilder.DropColumn(
                name: "KeyboardMode",
                table: "UnregisteredSales");

            migrationBuilder.DropColumn(
                name: "TankId",
                table: "UnregisteredSales");

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
                table: "UnregisteredSales",
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
                value: new DateTime(2024, 12, 2, 20, 41, 18, 299, DateTimeKind.Local).AddTicks(2959));

            migrationBuilder.AddForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales");

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
                table: "UnregisteredSales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "KeyboardMode",
                table: "UnregisteredSales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TankId",
                table: "UnregisteredSales",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 28, 22, 26, 26, 788, DateTimeKind.Local).AddTicks(7744));

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_TankId",
                table: "UnregisteredSales",
                column: "TankId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnregisteredSales_Nozzles_NozzleId",
                table: "UnregisteredSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnregisteredSales_Tanks_TankId",
                table: "UnregisteredSales",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id");
        }
    }
}
