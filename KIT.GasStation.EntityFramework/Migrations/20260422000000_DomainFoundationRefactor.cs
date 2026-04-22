using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DomainFoundationRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Users: добавить PasswordHash + PasswordSalt (хеширование паролей) ──

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            // Обновить seed-данные: хеш пароля "1" (PBKDF2-SHA256)
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt", "Password" },
                values: new object[]
                {
                    "1AsEPy/H6ECCu4ibA2nYx6cBRIG6D5403b0vSzRr2M0=",
                    "n6MSa06H0pxRoLsj9GyOEHLVRDvIkaJ+YwX4QS2bflU=",
                    null
                });

            // ── Shifts: открывший/закрывший, остатки кассы ──────────────────────

            migrationBuilder.AddColumn<string>(
                name: "OpenedBy",
                table: "Shifts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                table: "Shifts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningCashBalance",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosingCashBalance",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: true);

            // ── FuelIntakes: поставщик, номер ТЦ, закупочная цена ───────────────

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "FuelIntakes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckNumber",
                table: "FuelIntakes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerLitre",
                table: "FuelIntakes",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            // ── Fuels: seed — добавить CreatedAt ────────────────────────────────

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Fuels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2024, 1, 1));

            // ── Создать таблицу CashOperations ───────────────────────────────────

            migrationBuilder.CreateTable(
                name: "CashOperations",
                columns: table => new
                {
                    Id          = table.Column<int>(type: "int", nullable: false)
                                       .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId     = table.Column<int>(type: "int", nullable: false),
                    Type        = table.Column<int>(type: "int", nullable: false),
                    Amount      = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashierName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FiscalDocument = table.Column<int>(type: "int", nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashOperations_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashOperations_ShiftId",
                table: "CashOperations",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashOperations_CreatedAt",
                table: "CashOperations",
                column: "CreatedAt");

            // ── Создать таблицу FuelPriceHistories ───────────────────────────────

            migrationBuilder.CreateTable(
                name: "FuelPriceHistories",
                columns: table => new
                {
                    Id        = table.Column<int>(type: "int", nullable: false)
                                     .Annotation("SqlServer:Identity", "1, 1"),
                    FuelId    = table.Column<int>(type: "int", nullable: false),
                    OldPrice  = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NewPrice  = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason    = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelPriceHistories_Fuels_FuelId",
                        column: x => x.FuelId,
                        principalTable: "Fuels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelPriceHistories_FuelId",
                table: "FuelPriceHistories",
                column: "FuelId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPriceHistories_ChangedAt",
                table: "FuelPriceHistories",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CashOperations");
            migrationBuilder.DropTable(name: "FuelPriceHistories");

            migrationBuilder.DropColumn(name: "PasswordHash",        table: "Users");
            migrationBuilder.DropColumn(name: "PasswordSalt",        table: "Users");
            migrationBuilder.DropColumn(name: "OpenedBy",            table: "Shifts");
            migrationBuilder.DropColumn(name: "ClosedBy",            table: "Shifts");
            migrationBuilder.DropColumn(name: "OpeningCashBalance",  table: "Shifts");
            migrationBuilder.DropColumn(name: "ClosingCashBalance",  table: "Shifts");
            migrationBuilder.DropColumn(name: "SupplierName",        table: "FuelIntakes");
            migrationBuilder.DropColumn(name: "TruckNumber",         table: "FuelIntakes");
            migrationBuilder.DropColumn(name: "PricePerLitre",       table: "FuelIntakes");
            migrationBuilder.DropColumn(name: "CreatedAt",           table: "Fuels");
        }
    }
}
