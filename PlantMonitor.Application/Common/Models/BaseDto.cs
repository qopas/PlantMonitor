namespace PlantMonitor.Application.Common.Models;

public abstract class BaseDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
