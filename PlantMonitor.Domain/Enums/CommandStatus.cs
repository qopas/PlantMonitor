namespace PlantMonitor.Domain.Enums;

public enum CommandStatus
{
    Pending,
    Sent,
    Executing,
    Completed,
    Failed,
    Expired
}
