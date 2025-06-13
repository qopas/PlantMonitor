using PlantMonitor.Domain.Common;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

public class Device : BaseEntity
{
    public string DeviceId { get; set; } = string.Empty; 
    public long UserId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.ESP32PlantMonitor;
    public string? FirmwareVersion { get; set; }
    public string? MacAddress { get; set; }
    public string? WifiSSID { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; } = false;
    public DeviceStatus Status { get; set; } = DeviceStatus.Active;
    public string? Location { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Plant? Plant { get; set; }
    public virtual ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
    public virtual ICollection<WateringEvent> WateringEvents { get; set; } = new List<WateringEvent>();
    public virtual ICollection<DeviceAlert> DeviceAlerts { get; set; } = new List<DeviceAlert>();
    public virtual ICollection<DeviceConfiguration> DeviceConfigurations { get; set; } = new List<DeviceConfiguration>();
    public virtual ICollection<ApiToken> ApiTokens { get; set; } = new List<ApiToken>();
    public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
}