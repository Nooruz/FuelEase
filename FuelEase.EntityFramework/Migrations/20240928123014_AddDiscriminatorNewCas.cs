using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscriminatorNewCas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 28, 18, 30, 13, 886, DateTimeKind.Local).AddTicks(233));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 9, 26, 19, 8, 29, 907, DateTimeKind.Local).AddTicks(8832));
        }
    }
}
