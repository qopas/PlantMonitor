using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;

public class ApiToken : BaseEntity
{
    public long DeviceId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? TokenName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Scopes { get; set; } = "{}"; // JSON - What the token can access

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
}