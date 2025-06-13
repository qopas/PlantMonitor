using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.Alerts.Commands;

public record CreateDeviceAlertCommand(
    string DeviceId,
    AlertType AlertType,
    AlertSeverity Severity,
    string Title,
    string Message
) : IRequest<ApiResponse<DeviceAlertDto>>;

public record ResolveAlertCommand(long AlertId, long UserId) : IRequest<ApiResponse>;
