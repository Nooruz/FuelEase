using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FuelColorToColorHex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Color",
                table: "Fuels",
                newName: "ColorHex");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 9, 22, 25, 26, 40, DateTimeKind.Local).AddTicks(673));

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
                f.Name AS Fuel, 
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
            migrationBuilder.RenameColumn(
                name: "ColorHex",
                table: "Fuels",
                newName: "Color");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 9, 20, 8, 8, 809, DateTimeKind.Local).AddTicks(2632));
        }
    }
}
