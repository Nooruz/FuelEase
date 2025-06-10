using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelEase.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CashRegisterType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MFName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Controllers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    ControllerType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controllers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasurements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fuels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasurementId = table.Column<int>(type: "int", nullable: false),
                    TNVED = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    ValueAddedTax = table.Column<bool>(type: "bit", nullable: false),
                    SalesTax = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fuels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fuels_UnitOfMeasurements_UnitOfMeasurementId",
                        column: x => x.UnitOfMeasurementId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserRoleId = table.Column<int>(type: "int", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelRevaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FuelId = table.Column<int>(type: "int", nullable: false),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelRevaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelRevaluations_Fuels_FuelId",
                        column: x => x.FuelId,
                        principalTable: "Fuels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FuelId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<double>(type: "float", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    MinimumSize = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tanks_Fuels_FuelId",
                        column: x => x.FuelId,
                        principalTable: "Fuels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelIntakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelIntakes_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelNozzles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ControllerId = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    Nozzle = table.Column<int>(type: "int", nullable: false),
                    NozzleArmType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelNozzles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelNozzles_Controllers_ControllerId",
                        column: x => x.ControllerId,
                        principalTable: "Controllers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelNozzles_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelNozzleCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuelNozzleId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Beginning = table.Column<double>(type: "float", nullable: false),
                    Ending = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelNozzleCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelNozzleCounters_FuelNozzles_FuelNozzleId",
                        column: x => x.FuelNozzleId,
                        principalTable: "FuelNozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelNozzleCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TankId = table.Column<int>(type: "int", nullable: true),
                    FuelNozzleId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    ReceivedQuantity = table.Column<double>(type: "float", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    FiscalDocument = table.Column<int>(type: "int", nullable: true),
                    FiscalModule = table.Column<int>(type: "int", nullable: true),
                    Check = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnCheck = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<int>(type: "int", nullable: true),
                    FuelSaleStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelSales_FuelNozzles_FuelNozzleId",
                        column: x => x.FuelNozzleId,
                        principalTable: "FuelNozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelSales_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelSales_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UnregisteredSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TankId = table.Column<int>(type: "int", nullable: true),
                    FuelNozzleId = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    KeyboardMode = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnregisteredSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnregisteredSales_FuelNozzles_FuelNozzleId",
                        column: x => x.FuelNozzleId,
                        principalTable: "FuelNozzles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UnregisteredSales_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnregisteredSales_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UnitOfMeasurements",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "литр" },
                    { 2, "метр куб." },
                    { 3, "кВт*ч" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Администратор" },
                    { 2, "Кассир" }
                });

            migrationBuilder.InsertData(
                table: "Fuels",
                columns: new[] { "Id", "Deleted", "Name", "Price", "SalesTax", "TNVED", "UnitOfMeasurementId", "ValueAddedTax" },
                values: new object[,]
                {
                    { 1, false, "АИ-92", 0m, 0.0, null, 1, false },
                    { 2, false, "АИ-95", 0m, 0.0, null, 1, false },
                    { 3, false, "АИ-98", 0m, 0.0, null, 1, false },
                    { 4, false, "АИ-100", 0m, 0.0, null, 1, false },
                    { 5, false, "ДТ", 0m, 0.0, null, 1, false }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedDate", "Deleted", "FullName", "IsAdmin", "Login", "Password", "UserRoleId" },
                values: new object[] { 1, new DateTime(2023, 12, 13, 14, 15, 36, 384, DateTimeKind.Local).AddTicks(7307), false, "Администратор", false, "админ", "1", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_FuelIntakes_TankId",
                table: "FuelIntakes",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzleCounters_FuelNozzleId",
                table: "FuelNozzleCounters",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzleCounters_ShiftId",
                table: "FuelNozzleCounters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzles_ControllerId",
                table: "FuelNozzles",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelNozzles_TankId",
                table: "FuelNozzles",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelRevaluations_FuelId",
                table: "FuelRevaluations",
                column: "FuelId");

            migrationBuilder.CreateIndex(
                name: "IX_Fuels_UnitOfMeasurementId",
                table: "Fuels",
                column: "UnitOfMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_FuelNozzleId",
                table: "FuelSales",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_ShiftId",
                table: "FuelSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_TankId",
                table: "FuelSales",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_UserId",
                table: "Shifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_FuelId",
                table: "Tanks",
                column: "FuelId");

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_FuelNozzleId",
                table: "UnregisteredSales",
                column: "FuelNozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_ShiftId",
                table: "UnregisteredSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_TankId",
                table: "UnregisteredSales",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserRoleId",
                table: "Users",
                column: "UserRoleId");

            migrationBuilder.Sql("CREATE VIEW [dbo].[NozzleMeterValueView] AS " +
                "SELECT n.Id, c.Id AS ControllerId, c.Name + '\\' + n.Name AS Name, COALESCE (SUM(s.Quantity), 0) AS Quantity " +
                "FROM dbo.FuelNozzles AS n INNER JOIN dbo.Controllers AS c ON n.ControllerId = c.Id LEFT OUTER JOIN " +
                "dbo.FuelSales AS s ON n.Id = s.FuelNozzleId GROUP BY n.Id, n.Name, c.Name, c.Id");

            migrationBuilder.Sql("CREATE VIEW [dbo].[TankFuelQuantityView] AS SELECT t.Id, t.Name, t.Size, f.Name AS Fuel, COALESCE" +
                "((SELECT SUM(Quantity) AS Quantity FROM dbo.FuelIntakes AS i WHERE (TankId = t.Id)), 0) - COALESCE" +
                "((SELECT SUM(ReceivedQuantity) AS Quantity FROM dbo.FuelSales AS s WHERE (TankId = t.Id) AND (PaymentType <> 6)), 0) AS " +
                "CurrentFuelQuantity, t.MinimumSize, f.Id AS FuelId FROM dbo.Tanks AS t INNER JOIN dbo.Fuels AS f ON t.FuelId = f.Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashRegisters");

            migrationBuilder.DropTable(
                name: "FuelIntakes");

            migrationBuilder.DropTable(
                name: "FuelNozzleCounters");

            migrationBuilder.DropTable(
                name: "FuelRevaluations");

            migrationBuilder.DropTable(
                name: "FuelSales");

            migrationBuilder.DropTable(
                name: "UnregisteredSales");

            migrationBuilder.DropTable(
                name: "FuelNozzles");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Controllers");

            migrationBuilder.DropTable(
                name: "Tanks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Fuels");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UnitOfMeasurements");
        }
    }
}
