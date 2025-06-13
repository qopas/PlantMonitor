using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendDeviceAlertAsync(long userId, long deviceId, string title, string message, AlertSeverity severity = AlertSeverity.Warning)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                DeviceId = deviceId,
                NotificationType = NotificationType.Alert,
                Title = title,
                Message = message,
                IsRead = false,
                IsSent = false,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { severity = severity.ToString() })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // TODO: Implement actual push notification sending
            // For now, just log it
            _logger.LogInformation("Device alert notification created for user {UserId}: {Title}", userId, title);

            // Mark as sent (in real implementation, this would happen after successful delivery)
            notification.IsSent = true;
            notification.SentVia = NotificationChannel.Push;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending device alert notification to user {UserId}", userId);
        }
    }

    public async Task SendPlantAlertAsync(long userId, long plantId, string title, string message)
    {
        try
        {
            var plant = await _context.Plants
                .Include(p => p.Device)
                .FirstOrDefaultAsync(p => p.Id == plantId);

            if (plant == null)
            {
                _logger.LogWarning("Plant {PlantId} not found for notification", plantId);
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                DeviceId = plant.DeviceId,
                NotificationType = NotificationType.Alert,
                Title = title,
                Message = message,
                IsRead = false,
                IsSent = false,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { plantId = plantId, plantName = plant.PlantName })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plant alert notification created for user {UserId}: {Title}", userId, title);

            // Mark as sent
            notification.IsSent = true;
            notification.SentVia = NotificationChannel.Push;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending plant alert notification to user {UserId}", userId);
        }
    }
}
