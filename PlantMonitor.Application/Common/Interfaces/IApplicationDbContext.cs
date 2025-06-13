using Microsoft.EntityFrameworkCore;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Device> Devices { get; }
    DbSet<Plant> Plants { get; }
    DbSet<SensorData> SensorData { get; }
    DbSet<WateringEvent> WateringEvents { get; }
    DbSet<DeviceAlert> DeviceAlerts { get; }
    DbSet<DeviceConfiguration> DeviceConfigurations { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<DailySummary> DailySummaries { get; }
    DbSet<PlantHealthScore> PlantHealthScores { get; }
    DbSet<ApiToken> ApiTokens { get; }
    DbSet<SystemLog> SystemLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
