using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        builder.Property(u => u.NotificationPreferences)
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DeviceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.FirmwareVersion)
            .HasMaxLength(20);

        builder.Property(d => d.MacAddress)
            .HasMaxLength(17);

        builder.Property(d => d.WifiSSID)
            .HasMaxLength(100);

        builder.Property(d => d.Location)
            .HasMaxLength(200);

        builder.HasIndex(d => d.DeviceId)
            .IsUnique();

        builder.HasIndex(d => d.UserId);

        // Relationships
        builder.HasOne(d => d.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PlantName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.PlantType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.PlantSpecies)
            .HasMaxLength(200);

        builder.Property(p => p.PlantImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(p => p.DeviceId)
            .IsUnique();

        // Relationships
        builder.HasOne(p => p.Device)
            .WithOne(d => d.Plant)
            .HasForeignKey<Plant>(p => p.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SensorDataConfiguration : IEntityTypeConfiguration<SensorData>
{
    public void Configure(EntityTypeBuilder<SensorData> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SoilMoisture)
            .HasColumnType("decimal(5,2)");

        builder.Property(s => s.WaterLevel)
            .HasColumnType("decimal(5,2)");

        builder.Property(s => s.Temperature)
            .HasColumnType("decimal(5,2)");

        builder.Property(s => s.Humidity)
            .HasColumnType("decimal(5,2)");

        builder.HasIndex(s => new { s.DeviceId, s.Timestamp });
        builder.HasIndex(s => s.Timestamp);

        // Relationships
        builder.HasOne(s => s.Device)
            .WithMany(d => d.SensorData)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WateringEventConfiguration : IEntityTypeConfiguration<WateringEvent>
{
    public void Configure(EntityTypeBuilder<WateringEvent> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.SoilMoistureBefore)
            .HasColumnType("decimal(5,2)");

        builder.Property(w => w.SoilMoistureAfter)
            .HasColumnType("decimal(5,2)");

        builder.Property(w => w.WaterLevelBefore)
            .HasColumnType("decimal(5,2)");

        builder.Property(w => w.WaterLevelAfter)
            .HasColumnType("decimal(5,2)");

        builder.Property(w => w.FailureReason)
            .HasMaxLength(500);

        builder.Property(w => w.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(w => new { w.DeviceId, w.Timestamp });
        builder.HasIndex(w => w.Timestamp);

        // Relationships
        builder.HasOne(w => w.Device)
            .WithMany(d => d.WateringEvents)
            .HasForeignKey(w => w.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Plant)
            .WithMany(p => p.WateringEvents)
            .HasForeignKey(w => w.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DeviceAlertConfiguration : IEntityTypeConfiguration<DeviceAlert>
{
    public void Configure(EntityTypeBuilder<DeviceAlert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.HasIndex(a => new { a.DeviceId, a.IsResolved });
        builder.HasIndex(a => a.CreatedAt);

        // Relationships
        builder.HasOne(a => a.Device)
            .WithMany(d => d.DeviceAlerts)
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ResolvedByUser)
            .WithMany()
            .HasForeignKey(a => a.ResolvedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ApiTokenConfiguration : IEntityTypeConfiguration<ApiToken>
{
    public void Configure(EntityTypeBuilder<ApiToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.TokenName)
            .HasMaxLength(100);

        builder.Property(t => t.Scopes)
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => new { t.DeviceId, t.IsActive });

        // Relationships
        builder.HasOne(t => t.Device)
            .WithMany(d => d.ApiTokens)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
