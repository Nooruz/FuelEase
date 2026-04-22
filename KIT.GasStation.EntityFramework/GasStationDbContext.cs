using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.Discounts;
using KIT.GasStation.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework;

public class GasStationDbContext : DbContext
{
    #region DbSets

    public DbSet<Fuel> Fuels { get; set; }
    public DbSet<Tank> Tanks { get; set; }
    public DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }
    public DbSet<FuelSale> FuelSales { get; set; }
    public DbSet<Nozzle> Nozzles { get; set; }
    public DbSet<FuelIntake> FuelIntakes { get; set; }
    public DbSet<TankFuelQuantityView> TankFuelQuantityViews { get; set; }
    public DbSet<FuelRevaluation> FuelRevaluations { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<UnregisteredSale> UnregisteredSales { get; set; }
    public DbSet<EventPanel> EventsPanel { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<DiscountFuel> DiscountFuels { get; set; }
    public DbSet<DiscountTariffPlan> DiscountTariffPlans { get; set; }
    public DbSet<DiscountSale> DiscountSales { get; set; }
    public DbSet<FiscalData> FiscalDatas { get; set; }
    public DbSet<ShiftCounter> ShiftCounters { get; set; }
    public DbSet<TankShiftCounter> TankShiftCounters { get; set; }

    /// <summary>Кассовые операции (внесение / изъятие / инкассация)</summary>
    public DbSet<CashOperation> CashOperations { get; set; }

    /// <summary>История изменений цен топлива</summary>
    public DbSet<FuelPriceHistory> FuelPriceHistories { get; set; }

    #endregion

    #region Constructor

    public GasStationDbContext(DbContextOptions<GasStationDbContext> options) : base(options) { }

    #endregion

    #region Model Configuration

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Справочники ───────────────────────────────────────────────────────

        modelBuilder.Entity<UnitOfMeasurement>().HasData(
            new UnitOfMeasurement { Id = 1, Name = "литр" },
            new UnitOfMeasurement { Id = 2, Name = "метр куб." },
            new UnitOfMeasurement { Id = 3, Name = "кВт*ч" });

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { Id = 1, Name = "Администратор" },
            new UserRole { Id = 2, Name = "Кассир" });

        modelBuilder.Entity<Fuel>().HasData(
            new Fuel { Id = 1, Name = "АИ-92", UnitOfMeasurementId = 1, ColorHex = "#F6B511", CreatedAt = new DateTime(2024, 1, 1) },
            new Fuel { Id = 2, Name = "АИ-95", UnitOfMeasurementId = 1, ColorHex = "#ED2D38", CreatedAt = new DateTime(2024, 1, 1) },
            new Fuel { Id = 3, Name = "АИ-98", UnitOfMeasurementId = 1, ColorHex = "#4FA800", CreatedAt = new DateTime(2024, 1, 1) },
            new Fuel { Id = 4, Name = "АИ-100", UnitOfMeasurementId = 1, ColorHex = "#FFD700", CreatedAt = new DateTime(2024, 1, 1) },
            new Fuel { Id = 5, Name = "ДТ",    UnitOfMeasurementId = 1, ColorHex = "#737373", CreatedAt = new DateTime(2024, 1, 1) });

        // Администратор по умолчанию — пароль "1" захеширован через PBKDF2-SHA256
        // Соль:  n6MSa06H0pxRoLsj9GyOEHLVRDvIkaJ+YwX4QS2bflU=
        // Хеш:   1AsEPy/H6ECCu4ibA2nYx6cBRIG6D5403b0vSzRr2M0=
#pragma warning disable CS0618
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            UserRoleId = 1,
            FullName = "Администратор",
            PasswordHash = "1AsEPy/H6ECCu4ibA2nYx6cBRIG6D5403b0vSzRr2M0=",
            PasswordSalt = "n6MSa06H0pxRoLsj9GyOEHLVRDvIkaJ+YwX4QS2bflU=",
            Password = null,
            CreatedDate = new DateTime(2024, 1, 1),
            CreatedAt = new DateTime(2024, 1, 1)
        });
#pragma warning restore CS0618

        // ── Связи ─────────────────────────────────────────────────────────────

        modelBuilder.Entity<Tank>()
            .HasMany(t => t.FuelSales)
            .WithOne(fs => fs.Tank)
            .HasForeignKey(fs => fs.TankId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FuelSale>()
            .HasOne(f => f.DiscountSale)
            .WithOne(d => d.FuelSale)
            .HasForeignKey<DiscountSale>(d => d.FuelSaleId);

        modelBuilder.Entity<FuelSale>()
            .HasOne(fd => fd.FiscalData)
            .WithOne(fs => fs.FuelSale)
            .HasForeignKey<FiscalData>(fd => fd.FuelSaleId);

        modelBuilder.Entity<DiscountFuel>()
            .HasKey(df => new { df.DiscountId, df.FuelId });

        modelBuilder.Entity<DiscountFuel>()
            .HasOne(df => df.Discount)
            .WithMany(d => d.DiscountFuels)
            .HasForeignKey(df => df.DiscountId);

        modelBuilder.Entity<DiscountFuel>()
            .HasOne(df => df.Fuel)
            .WithMany(f => f.DiscountFuels)
            .HasForeignKey(df => df.FuelId);

        // CashOperation → Shift (без каскадного удаления)
        modelBuilder.Entity<CashOperation>()
            .HasOne(co => co.Shift)
            .WithMany(s => s.CashOperations)
            .HasForeignKey(co => co.ShiftId)
            .OnDelete(DeleteBehavior.NoAction);

        // FuelPriceHistory → Fuel
        modelBuilder.Entity<FuelPriceHistory>()
            .HasOne(ph => ph.Fuel)
            .WithMany(f => f.PriceHistory)
            .HasForeignKey(ph => ph.FuelId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Views ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<TankFuelQuantityView>(e =>
        {
            e.HasNoKey();
            e.ToView("TankFuelQuantityView");
        });

        modelBuilder.Entity<NozzleMeterValueView>(e =>
        {
            e.HasNoKey();
            e.ToView("NozzleMeterValueView");
        });

        base.OnModelCreating(modelBuilder);
    }

    #endregion
}
