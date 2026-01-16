using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.Discounts;
using KIT.GasStation.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework
{
    public class GasStationDbContext : DbContext
    {
        #region Public Properties

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

        #endregion

        #region Constructor

        public GasStationDbContext(DbContextOptions<GasStationDbContext> options) : base(options) { }

        #endregion

        #region Public Voids

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<UnitOfMeasurement>().HasData(
                new UnitOfMeasurement { Id = 1, Name = "литр" },
                new UnitOfMeasurement { Id = 2, Name = "метр куб." });

            _ = modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, Name = "Администратор" },
                new UserRole { Id = 2, Name = "Кассир" });

            _ = modelBuilder.Entity<Fuel>().HasData(
                new Fuel { Id = 1, Name = "АИ-92", UnitOfMeasurementId = 1, ColorHex = "#F6B511" },
                new Fuel { Id = 2, Name = "АИ-95", UnitOfMeasurementId = 1, ColorHex = "#ED2D38" },
                new Fuel { Id = 3, Name = "АИ-98", UnitOfMeasurementId = 1, ColorHex = "#4FA800" },
                new Fuel { Id = 4, Name = "АИ-100", UnitOfMeasurementId = 1, ColorHex = "#FFD700" },
                new Fuel { Id = 5, Name = "ДТ", UnitOfMeasurementId = 1, ColorHex = "#737373" });

            _ = modelBuilder.Entity<User>().HasData(
                new User { Id = 1, UserRoleId = 1, FullName = "Администратор", Password = "1", CreatedDate = DateTime.Now });

            _ = modelBuilder.Entity<Tank>()
                .HasMany(f => f.FuelSales)
                .WithOne(t => t.Tank)
                .HasForeignKey(f => f.TankId)
                .OnDelete(DeleteBehavior.NoAction);


            _ = modelBuilder.Entity<TankFuelQuantityView>(e =>
            {
                e.HasNoKey();
                e.ToView("TankFuelQuantityView");
            });

            _ = modelBuilder.Entity<NozzleMeterValueView>(e =>
            {
                e.HasNoKey();
                e.ToView("NozzleMeterValueView");
            });

            // Настройка связи многие ко многим с использованием промежуточной таблицы
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

            modelBuilder.Entity<FuelSale>()
                .HasOne(f => f.DiscountSale) // Навигационное свойство в FuelSale
                .WithOne(d => d.FuelSale)    // Навигационное свойство в DiscountSale
                .HasForeignKey<DiscountSale>(d => d.FuelSaleId); // Внешний ключ в DiscountSale

            modelBuilder.Entity<FuelSale>()
                .HasOne(fd => fd.FiscalData) // Навигационное свойство в FuelSale
                .WithOne(fs => fs.FuelSale)    // Навигационное свойство в FiscalData
                .HasForeignKey<FiscalData>(fd => fd.FuelSaleId); // Внешний ключ в FiscalData

            base.OnModelCreating(modelBuilder);
        }

        #endregion
    }
}
