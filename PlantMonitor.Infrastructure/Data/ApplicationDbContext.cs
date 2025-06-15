using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using System.Reflection;
using DeviceConfiguration = PlantMonitor.Domain.Entities.DeviceConfiguration;

namespace PlantMonitor.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole<long>, long>(options), IApplicationDbContext
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<WateringEvent> WateringEvents => Set<WateringEvent>();
    public DbSet<DeviceAlert> DeviceAlerts => Set<DeviceAlert>();
    public DbSet<DeviceConfiguration> DeviceConfigurations => Set<DeviceConfiguration>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<PlantHealthScore> PlantHealthScores => Set<PlantHealthScore>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ConfigureIdentityTables(modelBuilder);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole<long>>().ToTable("Roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<long>>().ToTable("UserRoles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<long>>().ToTable("UserClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<long>>().ToTable("UserLogins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<long>>().ToTable("UserTokens");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<long>>().ToTable("RoleClaims");
    }
}

