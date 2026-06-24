using Microsoft.EntityFrameworkCore;

namespace TempTest.Infrastructure.Persistence;

public sealed class TempTestDbContext(DbContextOptions<TempTestDbContext> options) : DbContext(options)
{
    public DbSet<Domain.SensorData.SensorData> SensorData => Set<Domain.SensorData.SensorData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.SensorData.SensorData>(entity =>
        {
            entity.ToTable("SensorData");

            entity.HasKey(sensorData => sensorData.Id);

            entity.Property(sensorData => sensorData.Id)
                .ValueGeneratedNever();

            entity.Property(sensorData => sensorData.Temperature)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(sensorData => sensorData.Humidity)
                .HasColumnType("decimal(5, 2)")
                .IsRequired();

            entity.Property(sensorData => sensorData.Timestamp)
                .IsRequired();

            entity.Property(sensorData => sensorData.CreatedAtUtc)
                .IsRequired();
        });
    }
}
