using PlantMonitor.Domain.Common;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Domain.Entities;

public class DeviceCommand : BaseEntity
{
    public long DeviceId { get; set; }
    public CommandType CommandType { get; set; }
    public string Parameters { get; set; } = "{}"; // JSON
    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? ExecutionResult { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    public int Priority { get; set; } = 1; // 1=Low, 2=Normal, 3=High, 4=Critical

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
}
