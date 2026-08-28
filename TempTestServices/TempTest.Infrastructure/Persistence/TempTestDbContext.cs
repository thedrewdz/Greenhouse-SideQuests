using Microsoft.EntityFrameworkCore;

namespace TempTest.Infrastructure.Persistence;

public sealed class TempTestDbContext(DbContextOptions<TempTestDbContext> options) : DbContext(options)
{
    public DbSet<Domain.SensorData.SensorData> SensorData => Set<Domain.SensorData.SensorData>();

    public DbSet<Domain.SprayEvent.SprayEvent> SprayEvents => Set<Domain.SprayEvent.SprayEvent>();

    public DbSet<Domain.FanEvent.FanEvent> FanEvents => Set<Domain.FanEvent.FanEvent>();

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

            entity.Property(sensorData => sensorData.ValveOn)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(sensorData => sensorData.FanOn)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(sensorData => sensorData.Timestamp)
                .IsRequired();

            entity.Property(sensorData => sensorData.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Domain.SprayEvent.SprayEvent>(entity =>
        {
            entity.ToTable("SprayEvents");

            entity.HasKey(sprayEvent => sprayEvent.Id);

            entity.Property(sprayEvent => sprayEvent.Id)
                .ValueGeneratedNever();

            entity.Property(sprayEvent => sprayEvent.StartTemperature)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.EndTemperature)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.StartHumidity)
                .HasColumnType("decimal(5, 2)")
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.EndHumidity)
                .HasColumnType("decimal(5, 2)")
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.StartDate)
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.EndDate)
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.WaterUsedMilliliters)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(sprayEvent => sprayEvent.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Domain.FanEvent.FanEvent>(entity =>
        {
            entity.ToTable("FanEvents");

            entity.HasKey(fanEvent => fanEvent.Id);

            entity.Property(fanEvent => fanEvent.Id)
                .ValueGeneratedNever();

            entity.Property(fanEvent => fanEvent.StartTemperature)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(fanEvent => fanEvent.EndTemperature)
                .HasColumnType("decimal(9, 2)")
                .IsRequired();

            entity.Property(fanEvent => fanEvent.StartHumidity)
                .HasColumnType("decimal(5, 2)")
                .IsRequired();

            entity.Property(fanEvent => fanEvent.EndHumidity)
                .HasColumnType("decimal(5, 2)")
                .IsRequired();

            entity.Property(fanEvent => fanEvent.StartDate)
                .IsRequired();

            entity.Property(fanEvent => fanEvent.EndDate)
                .IsRequired();

            entity.Property(fanEvent => fanEvent.CreatedAtUtc)
                .IsRequired();
        });
    }
}
