using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;

public class SensorData : BaseEntity
{
    public long DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; } // 0.00 to 100.00
    public decimal WaterLevel { get; set; } // 0.00 to 100.00
    public int? SoilMoistureRaw { get; set; } // Raw ADC value
    public int? WaterLevelRaw { get; set; } // Raw ADC value
    public decimal? Temperature { get; set; } // Optional temperature sensor
    public decimal? Humidity { get; set; } // Optional humidity sensor
    public int? LightLevel { get; set; } // Optional light sensor
    public bool IsValid { get; set; } = true;

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
}