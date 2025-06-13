using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;

public class Plant : BaseEntity
{
    public long DeviceId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantType { get; set; } = string.Empty; // Snake Plant, Peace Lily
    public string? PlantSpecies { get; set; } // Scientific name
    public int? AgeWeeks { get; set; }
    public int MoistureThresholdLow { get; set; } = 30; // % when to start watering
    public int MoistureThresholdHigh { get; set; } = 70; // % when to stop watering
    public int WateringDuration { get; set; } = 10; // seconds
    public bool AutoWateringEnabled { get; set; } = true;
    public string? PlantImageUrl { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
    public virtual ICollection<WateringEvent> WateringEvents { get; set; } = new List<WateringEvent>();
    public virtual ICollection<PlantHealthScore> PlantHealthScores { get; set; } = new List<PlantHealthScore>();
}