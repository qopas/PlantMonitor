using Microsoft.AspNetCore.Identity;

namespace PlantMonitor.Domain.Entities;

public class User : IdentityUser<long>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public string NotificationPreferences { get; set; } = "{}"; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}