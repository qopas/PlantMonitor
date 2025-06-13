using PlantMonitor.Domain.Common;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Domain.Entities;

public class DeviceAlert : BaseEntity
{
    public long DeviceId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public long? ResolvedBy { get; set; } // user_id who resolved it
    public string Metadata { get; set; } = "{}"; // JSON

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
    public virtual User? ResolvedByUser { get; set; }
}