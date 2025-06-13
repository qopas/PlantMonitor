using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;

public class DailySummary : BaseEntity
{
    public long DeviceId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? AvgSoilMoisture { get; set; }
    public decimal? MinSoilMoisture { get; set; }
    public decimal? MaxSoilMoisture { get; set; }
    public decimal? AvgWaterLevel { get; set; }
    public decimal? MinWaterLevel { get; set; }
    public int WateringCount { get; set; } = 0;
    public int TotalWateringDuration { get; set; } = 0; // seconds
    public int EstimatedWaterUsed { get; set; } = 0; // ml
    public int DataPointsCount { get; set; } = 0;

    // Navigation properties
    public virtual Device Device { get; set; } = null!;
}