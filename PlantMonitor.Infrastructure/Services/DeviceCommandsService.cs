using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Infrastructure.Services;

public class DeviceCommandsService(IApplicationDbContext context, ILogger<DeviceCommandsService> logger)
    : IDeviceCommandsService
{
    public async Task<DeviceCommand> CreateCommandAsync(long deviceId, CommandType commandType, object parameters, int priority = 1)
    {
        var command = new DeviceCommand
        {
            DeviceId = deviceId,
            CommandType = commandType,
            Parameters = JsonSerializer.Serialize(parameters),
            Status = CommandStatus.Pending,
            Priority = priority,
            ExpiresAt = GetExpirationTime(commandType)
        };

        context.DeviceCommands.Add(command);
        await context.SaveChangesAsync();

        logger.LogInformation("Created command {CommandType} for device {DeviceId} with priority {Priority}", 
            commandType, deviceId, priority);

        return command;
    }

    public async Task<List<DeviceCommand>> GetPendingCommandsAsync(string deviceId)
    {
        var commands = await context.DeviceCommands
            .Include(c => c.Device)
            .Where(c => c.Device.DeviceId == deviceId && 
                       c.Status == CommandStatus.Pending && 
                       c.ExpiresAt > DateTime.UtcNow)
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.CreatedAt)
            .Take(10) // Limit to 10 commands per poll
            .ToListAsync();

        // Mark commands as sent
        foreach (var command in commands)
        {
            command.Status = CommandStatus.Sent;
        }

        if (commands.Any())
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Sent {Count} pending commands to device {DeviceId}", commands.Count, deviceId);
        }

        return commands;
    }

    public async Task<bool> AcknowledgeCommandAsync(long commandId, bool success, string? result = null, string? errorMessage = null)
    {
        var command = await context.DeviceCommands.FindAsync(commandId);
        if (command == null)
        {
            logger.LogWarning("Command {CommandId} not found for acknowledgment", commandId);
            return false;
        }

        command.Status = success ? CommandStatus.Completed : CommandStatus.Failed;
        command.AcknowledgedAt = DateTime.UtcNow;
        command.ExecutionResult = result;
        command.ErrorMessage = errorMessage;

        await context.SaveChangesAsync();

        logger.LogInformation("Command {CommandId} acknowledged with status {Status}", 
            commandId, command.Status);

        return true;
    }

    public async Task<List<DeviceCommand>> GetDeviceCommandHistoryAsync(long deviceId, int take = 50)
    {
        return await context.DeviceCommands
            .Where(c => c.DeviceId == deviceId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task CleanupExpiredCommandsAsync()
    {
        var expiredCommands = await context.DeviceCommands
            .Where(c => c.ExpiresAt < DateTime.UtcNow && 
                       (c.Status == CommandStatus.Pending || c.Status == CommandStatus.Sent))
            .ToListAsync();

        foreach (var command in expiredCommands)
        {
            command.Status = CommandStatus.Expired;
        }

        if (expiredCommands.Any())
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Marked {Count} expired commands", expiredCommands.Count);
        }
    }

    private static DateTime GetExpirationTime(CommandType commandType)
    {
        return commandType switch
        {
            CommandType.EmergencyStop => DateTime.UtcNow.AddMinutes(2), // Critical
            CommandType.ManualWatering => DateTime.UtcNow.AddMinutes(5), // Important
            CommandType.UpdateConfiguration => DateTime.UtcNow.AddMinutes(30), // Can wait
            _ => DateTime.UtcNow.AddMinutes(10) // Default
        };
    }
}
