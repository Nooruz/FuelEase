using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateDiscountService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FuelSales_ShiftId",
                table: "FuelSales");

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DocumentCounters",
                columns: table => new
                {
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCounters", x => new { x.DocumentType, x.PeriodKey });
                });

            migrationBuilder.Sql(@"
                WITH ShiftCTE AS
                (
                    SELECT Id,
                           OpeningDate,
                           [Year] = YEAR(OpeningDate),
                           RN = ROW_NUMBER() OVER
                           (
                               PARTITION BY YEAR(OpeningDate)
                               ORDER BY OpeningDate, Id
                           )
                    FROM Shifts
                )
                UPDATE s
                SET s.[Year] = c.[Year],
                    s.[Number] = c.RN
                FROM Shifts s
                INNER JOIN ShiftCTE c ON s.Id = c.Id;
            ");

            // 4. Перенумеровать существующие FuelSales внутри каждой смены
            migrationBuilder.Sql(@"
                    WITH FuelSaleCTE AS
                    (
                        SELECT Id,
                               ShiftId,
                               RN = ROW_NUMBER() OVER
                               (
                                   PARTITION BY ShiftId
                                   ORDER BY CreateDate, Id
                               )
                        FROM FuelSales
                    )
                    UPDATE fs
                    SET fs.[Number] = c.RN
                    FROM FuelSales fs
                    INNER JOIN FuelSaleCTE c ON fs.Id = c.Id;
                ");

            // 5. Заполнить DocumentCounters для Shift
            migrationBuilder.Sql(@"
                INSERT INTO DocumentCounters (DocumentType, PeriodKey, CurrentValue)
                SELECT
                    'Shift',
                    CAST([Year] AS nvarchar(20)),
                    MAX([Number])
                FROM Shifts
                GROUP BY [Year];
            ");

            // 6. Заполнить DocumentCounters для FuelSale
            migrationBuilder.Sql(@"
                INSERT INTO DocumentCounters (DocumentType, PeriodKey, CurrentValue)
                SELECT
                    'FuelSale',
                    CONCAT('SHIFT-', CAST(ShiftId AS nvarchar(20))),
                    MAX([Number])
                FROM FuelSales
                GROUP BY ShiftId;
            ");


            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Year_Number",
                table: "Shifts",
                columns: new[] { "Year", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_ShiftId_Number",
                table: "FuelSales",
                columns: new[] { "ShiftId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentCounters");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_Year_Number",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_FuelSales_ShiftId_Number",
                table: "FuelSales");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "FuelSales");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_ShiftId",
                table: "FuelSales",
                column: "ShiftId");
        }
    }
}
