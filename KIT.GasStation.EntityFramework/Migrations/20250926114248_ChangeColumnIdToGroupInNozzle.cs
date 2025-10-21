using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KIT.GasStation.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColumnIdToGroupInNozzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalDocument = table.Column<int>(type: "int", nullable: true),
                    FiscalModule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Check = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnCheck = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDatas", x => x.Id);
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
                name: "DiscountTariffPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinimumValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaximumValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountTariffPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountTariffPlans_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    SalesTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "DiscountFuels",
                columns: table => new
                {
                    DiscountId = table.Column<int>(type: "int", nullable: false),
                    FuelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountFuels", x => new { x.DiscountId, x.FuelId });
                    table.ForeignKey(
                        name: "FK_DiscountFuels_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountFuels_Fuels_FuelId",
                        column: x => x.FuelId,
                        principalTable: "Fuels",
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
                    Number = table.Column<int>(type: "int", nullable: false),
                    FuelId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    MinimumSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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
                name: "Nozzles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tube = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nozzles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nozzles_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventsPanel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EventEntity = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventsPanel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventsPanel_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
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
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelIntakes_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuelIntakes_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TankShiftCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    BeginCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TankShiftCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TankShiftCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TankShiftCounters_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    DiscountSaleId = table.Column<int>(type: "int", nullable: true),
                    FiscalDataId = table.Column<int>(type: "int", nullable: true),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerSum = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChangeSum = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    IsForSum = table.Column<bool>(type: "bit", nullable: false),
                    FuelSaleStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelSales_FiscalDatas_FiscalDataId",
                        column: x => x.FiscalDataId,
                        principalTable: "FiscalDatas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FuelSales_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
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
                name: "ShiftCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    BeginNozzleCounter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndNozzleCounter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BeginSaleCounter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndSaleCounter = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftCounters_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftCounters_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnregisteredSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NozzleId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnregisteredSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnregisteredSales_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnregisteredSales_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuelSaleId = table.Column<int>(type: "int", nullable: false),
                    DiscountId = table.Column<int>(type: "int", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountSum = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountSales_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountSales_FuelSales_FuelSaleId",
                        column: x => x.FuelSaleId,
                        principalTable: "FuelSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                columns: new[] { "Id", "ColorHex", "Deleted", "Name", "Price", "SalesTax", "TNVED", "UnitOfMeasurementId", "ValueAddedTax" },
                values: new object[,]
                {
                    { 1, "#F6B511", false, "АИ-92", 0m, 0m, null, 1, false },
                    { 2, "#ED2D38", false, "АИ-95", 0m, 0m, null, 1, false },
                    { 3, "#4FA800", false, "АИ-98", 0m, 0m, null, 1, false },
                    { 4, "#FFD700", false, "АИ-100", 0m, 0m, null, 1, false },
                    { 5, "#737373", false, "ДТ", 0m, 0m, null, 1, false }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedDate", "Deleted", "FullName", "IsAdmin", "Password", "UserRoleId" },
                values: new object[] { 1, new DateTime(2025, 9, 26, 17, 42, 47, 629, DateTimeKind.Local).AddTicks(3984), false, "Администратор", false, "1", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountFuels_FuelId",
                table: "DiscountFuels",
                column: "FuelId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountSales_DiscountId",
                table: "DiscountSales",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountSales_FuelSaleId",
                table: "DiscountSales",
                column: "FuelSaleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountTariffPlans_DiscountId",
                table: "DiscountTariffPlans",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_EventsPanel_ShiftId",
                table: "EventsPanel",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelIntakes_ShiftId",
                table: "FuelIntakes",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelIntakes_TankId",
                table: "FuelIntakes",
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
                name: "IX_FuelSales_FiscalDataId",
                table: "FuelSales",
                column: "FiscalDataId",
                unique: true,
                filter: "[FiscalDataId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_NozzleId",
                table: "FuelSales",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_ShiftId",
                table: "FuelSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelSales_TankId",
                table: "FuelSales",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_Nozzles_TankId",
                table: "Nozzles",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCounters_NozzleId",
                table: "ShiftCounters",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCounters_ShiftId",
                table: "ShiftCounters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_UserId",
                table: "Shifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_FuelId",
                table: "Tanks",
                column: "FuelId");

            migrationBuilder.CreateIndex(
                name: "IX_TankShiftCounters_ShiftId",
                table: "TankShiftCounters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_TankShiftCounters_TankId",
                table: "TankShiftCounters",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_NozzleId",
                table: "UnregisteredSales",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_UnregisteredSales_ShiftId",
                table: "UnregisteredSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserRoleId",
                table: "Users",
                column: "UserRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountFuels");

            migrationBuilder.DropTable(
                name: "DiscountSales");

            migrationBuilder.DropTable(
                name: "DiscountTariffPlans");

            migrationBuilder.DropTable(
                name: "EventsPanel");

            migrationBuilder.DropTable(
                name: "FuelIntakes");

            migrationBuilder.DropTable(
                name: "FuelRevaluations");

            migrationBuilder.DropTable(
                name: "ShiftCounters");

            migrationBuilder.DropTable(
                name: "TankShiftCounters");

            migrationBuilder.DropTable(
                name: "UnregisteredSales");

            migrationBuilder.DropTable(
                name: "FuelSales");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "FiscalDatas");

            migrationBuilder.DropTable(
                name: "Nozzles");

            migrationBuilder.DropTable(
                name: "Shifts");

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
