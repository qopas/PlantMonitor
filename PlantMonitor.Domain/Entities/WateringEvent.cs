using PlantMonitor.Domain.Common;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Domain.Entities;

public class WateringEvent : BaseEntity
{
    public long DeviceId { get; set; }
    public long? PlantId { get; set; }
    public DateTime Timestamp { get; set; }
    public TriggerType TriggerType { get; set; }
    public decimal? SoilMoistureBefore { get; set; }
    public decimal? SoilMoistureAfter { get; set; }
    public decimal? WaterLevelBefore { get; set; }
    public decimal? WaterLevelAfter { get; set; }
    public int DurationSeconds { get; set; }
    public int? WaterAmountMl { get; set; } // Calculated/estimated
    public bool WasSuccessful { get; set; } = true;
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
    public virtual Plant? Plant { get; set; }
}