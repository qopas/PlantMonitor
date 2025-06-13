using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Alerts.Queries;

public record GetDeviceAlertsQuery(long DeviceId, bool IncludeResolved = false) : IRequest<ApiResponse<List<DeviceAlertDto>>>;

public record GetUserAlertsQuery(long UserId, bool IncludeResolved = false) : IRequest<ApiResponse<List<DeviceAlertDto>>>;
