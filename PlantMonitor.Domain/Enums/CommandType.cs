namespace PlantMonitor.Domain.Enums;

public enum CommandType
{
    UpdateConfiguration,
    ManualWatering,
    EmergencyStop,
    Restart,
    CalibrateSensors,
    EnableAutoWatering,
    DisableAutoWatering
}
