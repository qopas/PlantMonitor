using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;
public class DeviceConfiguration : BaseEntity
{
    public long DeviceId { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty; // JSON
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
}