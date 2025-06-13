using PlantMonitor.Domain.Common;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

public class Notification : BaseEntity
{
    public long UserId { get; set; }
    public long? DeviceId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public bool IsSent { get; set; } = false;
    public NotificationChannel? SentVia { get; set; }
    public string Metadata { get; set; } = "{}"; // JSON
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Device? Device { get; set; }
}