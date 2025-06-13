using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendDeviceAlertAsync(long userId, long deviceId, string title, string message, AlertSeverity severity = AlertSeverity.Warning);
    Task SendPlantAlertAsync(long userId, long plantId, string title, string message);
}
