using PlantMonitor.Application.Common.Models;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.DTOs;

public class DeviceDto : BaseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Location { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public DeviceStatus Status { get; set; }
    public PlantDto? Plant { get; set; }
}

public class PlantDto : BaseDto
{
    public long DeviceId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantType { get; set; } = string.Empty;
    public string? PlantSpecies { get; set; }
    public int? AgeWeeks { get; set; }
    public int MoistureThresholdLow { get; set; }
    public int MoistureThresholdHigh { get; set; }
    public int WateringDuration { get; set; }
    public bool AutoWateringEnabled { get; set; }
    public string? PlantImageUrl { get; set; }
    public string? Notes { get; set; }
}

public class SensorDataDto : BaseDto
{
    public long DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal WaterLevel { get; set; }
    public int? SoilMoistureRaw { get; set; }
    public int? WaterLevelRaw { get; set; }
    public decimal? Temperature { get; set; }
    public bool IsValid { get; set; }
}

public class WateringEventDto : BaseDto
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
    public bool WasSuccessful { get; set; }
    public string? FailureReason { get; set; }
}

public class DeviceAlertDto : BaseDto
{
    public long DeviceId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class UserDto : BaseDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; }
}
