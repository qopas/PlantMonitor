using Microsoft.Extensions.Logging;
using PlantMonitor.Domain.Common;
using LogLevel = PlantMonitor.Domain.Enums.LogLevel;

namespace PlantMonitor.Domain.Entities;

public class SystemLog : BaseEntity
{
    public long? DeviceId { get; set; }
    public LogLevel LogLevel { get; set; }
    public string? Component { get; set; } // SensorManager, PumpController, etc.
    public string? EventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}"; // JSON
    public DateTime Timestamp { get; set; }

    // Navigation properties
    public virtual Device? Device { get; set; }
}