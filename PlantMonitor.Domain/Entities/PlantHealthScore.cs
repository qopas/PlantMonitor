using PlantMonitor.Domain.Common;

namespace PlantMonitor.Domain.Entities;

public class PlantHealthScore : BaseEntity
{
    public long PlantId { get; set; }
    public DateOnly Date { get; set; }
    public int HealthScore { get; set; } // 0-100
    public int? MoistureScore { get; set; } // 0-100
    public int? WateringEfficiencyScore { get; set; } // 0-100
    public int? ConsistencyScore { get; set; } // 0-100
    public string Factors { get; set; } = "{}"; // JSON - What influenced the score
    public string? Recommendations { get; set; }

    // Navigation properties
    public virtual Plant Plant { get; set; } = null!;
}