using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Common.Interfaces;

public interface IDeviceCommandsService
{
    Task<DeviceCommand> CreateCommandAsync(long deviceId, CommandType commandType, object parameters, int priority = 1);
    Task<List<DeviceCommand>> GetPendingCommandsAsync(string deviceId);
    Task<bool> AcknowledgeCommandAsync(long commandId, bool success, string? result = null, string? errorMessage = null);
    Task<List<DeviceCommand>> GetDeviceCommandHistoryAsync(long deviceId, int take = 50);
    Task CleanupExpiredCommandsAsync();
}
