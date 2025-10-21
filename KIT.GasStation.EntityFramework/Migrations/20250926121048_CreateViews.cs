using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW NozzleMeterValueView AS
                SELECT
                    t.Id,
                    t.Name,
                    COALESCE(SUM(s.ReceivedQuantity), 0) AS Quantity
                FROM dbo.FuelSales AS s
                INNER JOIN dbo.Nozzles AS n ON s.NozzleId = n.Id
                INNER JOIN dbo.Tanks   AS t ON s.TankId = t.Id AND n.TankId = t.Id
                GROUP BY t.Id, t.Name;
                ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW TankFuelQuantityView AS
                SELECT
                    t.Id,
                    t.Name,
                    t.Size,
                    f.Name AS Fuel,
                    t.Number,
                    COALESCE( (SELECT SUM(i.Quantity)         FROM dbo.FuelIntakes AS i WHERE i.TankId = t.Id), 0)
                  - COALESCE( (SELECT SUM(s.ReceivedQuantity) FROM dbo.FuelSales   AS s WHERE s.TankId = t.Id AND s.PaymentType <> 6), 0)
                    AS CurrentFuelQuantity,
                    t.MinimumSize,
                    f.Id AS FuelId,
                    f.ColorHex
                FROM dbo.Tanks AS t
                INNER JOIN dbo.Fuels AS f ON t.FuelId = f.Id;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID('NozzleMeterValueView','V') IS NOT NULL DROP VIEW NozzleMeterValueView;");
            migrationBuilder.Sql(@"IF OBJECT_ID('TankFuelQuantityView','V')   IS NOT NULL DROP VIEW TankFuelQuantityView;");
        }
    }
}
