using KIT.GasStation.LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.LicenseServer.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<LicenseRecord> Licenses => Set<LicenseRecord>();
    public DbSet<ActivationRecord> Activations => Set<ActivationRecord>();
    public DbSet<HeartbeatLog> HeartbeatLogs => Set<HeartbeatLog>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LicenseRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.HasIndex(e => e.CustomerId);
        });

        modelBuilder.Entity<ActivationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => new { e.LicenseId, e.HardwareId }).IsUnique();
        });

        modelBuilder.Entity<HeartbeatLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => e.Timestamp);
        });

        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
