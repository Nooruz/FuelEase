using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFiscalDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalDiscount_FiscalDatas_FiscalDataId",
                table: "FiscalDiscount");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FiscalDiscount",
                table: "FiscalDiscount");

            migrationBuilder.RenameTable(
                name: "FiscalDiscount",
                newName: "FiscalDiscounts");

            migrationBuilder.RenameIndex(
                name: "IX_FiscalDiscount_FiscalDataId",
                table: "FiscalDiscounts",
                newName: "IX_FiscalDiscounts_FiscalDataId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FiscalDiscounts",
                table: "FiscalDiscounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalDiscounts_FiscalDatas_FiscalDataId",
                table: "FiscalDiscounts",
                column: "FiscalDataId",
                principalTable: "FiscalDatas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalDiscounts_FiscalDatas_FiscalDataId",
                table: "FiscalDiscounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FiscalDiscounts",
                table: "FiscalDiscounts");

            migrationBuilder.RenameTable(
                name: "FiscalDiscounts",
                newName: "FiscalDiscount");

            migrationBuilder.RenameIndex(
                name: "IX_FiscalDiscounts_FiscalDataId",
                table: "FiscalDiscount",
                newName: "IX_FiscalDiscount_FiscalDataId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FiscalDiscount",
                table: "FiscalDiscount",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalDiscount_FiscalDatas_FiscalDataId",
                table: "FiscalDiscount",
                column: "FiscalDataId",
                principalTable: "FiscalDatas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
