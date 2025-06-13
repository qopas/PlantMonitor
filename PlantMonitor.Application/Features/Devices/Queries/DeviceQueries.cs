using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Devices.Queries;

public record GetUserDevicesQuery(long UserId) : IRequest<ApiResponse<List<DeviceDto>>>;

public record GetDeviceByIdQuery(long DeviceId) : IRequest<ApiResponse<DeviceDto>>;

public record GetDeviceByDeviceIdQuery(string DeviceId) : IRequest<ApiResponse<DeviceDto>>;

public record GetDeviceConfigQuery(string DeviceId) : IRequest<ApiResponse<object>>;
