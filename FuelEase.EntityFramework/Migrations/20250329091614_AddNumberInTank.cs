using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberInTank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Tanks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 29, 15, 16, 14, 205, DateTimeKind.Local).AddTicks(3125));

            // Удаляем представление TankFuelQuantityView, если оно существует
            migrationBuilder.Sql("DROP VIEW IF EXISTS TankFuelQuantityView");

            // Создаем представление TankFuelQuantityView с новой схемой,
            // где вместо столбца Color теперь используется ColorHex
            migrationBuilder.Sql(@"
            CREATE VIEW TankFuelQuantityView AS
            SELECT 
                t.Id, 
                t.Name, 
                t.Size, 
                f.Name AS Fuel, t.Number, 
                COALESCE(
                    (SELECT SUM(Quantity) FROM dbo.FuelIntakes AS i WHERE TankId = t.Id), 0
                ) - COALESCE(
                    (SELECT SUM(ReceivedQuantity) FROM dbo.FuelSales AS s WHERE TankId = t.Id AND PaymentType <> 6), 0
                ) AS CurrentFuelQuantity,
                t.MinimumSize, 
                f.Id AS FuelId, 
                f.ColorHex
            FROM dbo.Tanks AS t
            INNER JOIN dbo.Fuels AS f ON t.FuelId = f.Id");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "Tanks");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 3, 25, 22, 3, 36, 154, DateTimeKind.Local).AddTicks(1156));
        }
    }
}
