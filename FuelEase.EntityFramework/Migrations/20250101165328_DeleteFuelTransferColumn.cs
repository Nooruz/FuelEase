using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFuelTransferColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales");

            migrationBuilder.DropForeignKey(
                name: "FK_Nozzles_FuelTransferColumns_FuelTransferColumnId",
                table: "Nozzles");

            migrationBuilder.DropTable(
                name: "FuelTransferColumns");

            migrationBuilder.DropIndex(
                name: "IX_Nozzles_FuelTransferColumnId",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "AutorunType",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "Blocked",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "BlockingChannelOperation",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ConstantFlowReduction",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ControlCounter",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "ControlRequest",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "CounterType",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "CounterValue",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "DisableControlFilling",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "FuelTransferColumnId",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "InvertStartStop",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "MechanicalCounters",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "NozzleAddress",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "NozzleSensor",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "OverflowConstant",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "PumpAccelerationTime",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "RequestCounter",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "RequestFromFuelTransferColumn",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "StartDelay",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "StartStopButton",
                table: "Nozzles");

            migrationBuilder.RenameColumn(
                name: "UNBKAddress",
                table: "Nozzles",
                newName: "Side");

            migrationBuilder.AlterColumn<int>(
                name: "TankId",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
                table: "FuelSales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 1, 1, 22, 53, 27, 342, DateTimeKind.Local).AddTicks(7556));

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales");

            migrationBuilder.RenameColumn(
                name: "Side",
                table: "Nozzles",
                newName: "UNBKAddress");

            migrationBuilder.AddColumn<int>(
                name: "AutorunType",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Blocked",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlockingChannelOperation",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConstantFlowReduction",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ControlCounter",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ControlRequest",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CounterType",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CounterValue",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DisableControlFilling",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FuelTransferColumnId",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvertStartStop",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MechanicalCounters",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NozzleAddress",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NozzleSensor",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverflowConstant",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PumpAccelerationTime",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequestCounter",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequestFromFuelTransferColumn",
                table: "Nozzles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartDelay",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartStopButton",
                table: "Nozzles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TankId",
                table: "FuelSales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "NozzleId",
                table: "FuelSales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "FuelTransferColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<int>(type: "int", nullable: false),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    EchoSuppression = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NozzlesOnSide = table.Column<int>(type: "int", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Side = table.Column<int>(type: "int", nullable: false),
                    SurveyPeriod = table.Column<int>(type: "int", nullable: false),
                    Timeout = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    USBPowerSupply = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTransferColumns", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 12, 2, 20, 41, 18, 299, DateTimeKind.Local).AddTicks(2959));

            migrationBuilder.CreateIndex(
                name: "IX_Nozzles_FuelTransferColumnId",
                table: "Nozzles",
                column: "FuelTransferColumnId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelSales_Nozzles_NozzleId",
                table: "FuelSales",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Nozzles_FuelTransferColumns_FuelTransferColumnId",
                table: "Nozzles",
                column: "FuelTransferColumnId",
                principalTable: "FuelTransferColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
