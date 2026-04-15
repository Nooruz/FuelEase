using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalDiscount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FiscalDataId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDiscount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDiscount_FiscalDatas_FiscalDataId",
                        column: x => x.FiscalDataId,
                        principalTable: "FiscalDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                column: "TNVED",
                value: "2710124130");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                column: "TNVED",
                value: "2710124500");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                column: "TNVED",
                value: "2710124500");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                column: "TNVED",
                value: "2710124900");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                column: "TNVED",
                value: "2710194210");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(1995, 3, 21, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDiscount_FiscalDataId",
                table: "FiscalDiscount",
                column: "FiscalDataId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalDiscount");

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                column: "TNVED",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                column: "TNVED",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                column: "TNVED",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                column: "TNVED",
                value: null);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                column: "TNVED",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 6, 11, 15, 36, 996, DateTimeKind.Local).AddTicks(1394));
        }
    }
}
