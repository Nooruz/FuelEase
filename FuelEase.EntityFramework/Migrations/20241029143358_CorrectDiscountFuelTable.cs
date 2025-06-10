using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CorrectDiscountFuelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelIds",
                table: "Discounts");

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DiscountFuels",
                columns: table => new
                {
                    DiscountId = table.Column<int>(type: "int", nullable: false),
                    FuelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountFuels", x => new { x.DiscountId, x.FuelId });
                    table.ForeignKey(
                        name: "FK_DiscountFuels_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountFuels_Fuels_FuelId",
                        column: x => x.FuelId,
                        principalTable: "Fuels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 10, 29, 20, 33, 58, 104, DateTimeKind.Local).AddTicks(5462));

            migrationBuilder.CreateIndex(
                name: "IX_DiscountFuels_FuelId",
                table: "DiscountFuels",
                column: "FuelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountFuels");

            migrationBuilder.DropColumn(
                name: "ChangeSum",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "CustomerSum",
                table: "FuelSales");

            migrationBuilder.AddColumn<string>(
                name: "FuelIds",
                table: "Discounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 10, 24, 22, 11, 31, 802, DateTimeKind.Local).AddTicks(441));
        }
    }
}
