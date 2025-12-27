using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeleteNozzleTankFuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "Tanks",
                newName: "IsDeleted");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tanks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tanks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tanks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Nozzles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Nozzles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Nozzles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Fuels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Fuels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Fuels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Fuels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, null });

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, null });

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, null });

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, null });

            migrationBuilder.UpdateData(
                table: "Fuels",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 12, 27, 12, 29, 57, 626, DateTimeKind.Local).AddTicks(9272));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tanks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tanks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tanks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Fuels");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Fuels");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Fuels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Fuels");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Tanks",
                newName: "Deleted");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 12, 1, 21, 6, 42, 411, DateTimeKind.Local).AddTicks(7085));
        }
    }
}
