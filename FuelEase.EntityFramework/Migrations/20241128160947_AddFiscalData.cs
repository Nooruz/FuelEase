using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Check",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "DiscountQuantity",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "DiscountSum",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "FiscalModule",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "ReturnCheck",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "FiscalDocument",
                table: "FuelSales",
                newName: "FiscalDataId");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ChangeSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "DiscountSaleId",
                table: "FuelSales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuelSaleId = table.Column<int>(type: "int", nullable: false),
                    DiscountId = table.Column<int>(type: "int", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountQuantity = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountSales_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountSales_FuelSales_FuelSaleId",
                        column: x => x.FuelSaleId,
                        principalTable: "FuelSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalDocument = table.Column<int>(type: "int", nullable: true),
                    FiscalModule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Check = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnCheck = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDatas", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 28, 22, 9, 46, 703, DateTimeKind.Local).AddTicks(3932));

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_FiscalDataId",
                table: "FuelSales",
                column: "FiscalDataId",
                unique: true,
                filter: "[FiscalDataId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountSales_DiscountId",
                table: "DiscountSales",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountSales_FuelSaleId",
                table: "DiscountSales",
                column: "FuelSaleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_FiscalDatas_FiscalDataId",
                table: "FuelSales",
                column: "FiscalDataId",
                principalTable: "FiscalDatas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_FiscalDatas_FiscalDataId",
                table: "FuelSales");

            migrationBuilder.DropTable(
                name: "DiscountSales");

            migrationBuilder.DropTable(
                name: "FiscalDatas");

            migrationBuilder.DropIndex(
                name: "IX_FuelSales_FiscalDataId",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "DiscountSaleId",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "FiscalDataId",
                table: "FuelSales",
                newName: "FiscalDocument");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ChangeSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Check",
                table: "FuelSales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "DiscountQuantity",
                table: "FuelSales",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountSum",
                table: "FuelSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FiscalModule",
                table: "FuelSales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "FuelSales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCheck",
                table: "FuelSales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 11, 25, 20, 8, 24, 129, DateTimeKind.Local).AddTicks(4886));
        }
    }
}
