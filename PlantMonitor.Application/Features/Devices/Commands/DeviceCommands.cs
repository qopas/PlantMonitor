using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(string DeviceId, string UserEmail, string? DeviceName = null) : IRequest<ApiResponse<DeviceDto>>;

public record UpdateDeviceCommand(
    long Id,
    string? DeviceName,
    string? Location,
    string? FirmwareVersion
) : IRequest<ApiResponse<DeviceDto>>;

public record UpdateDeviceStatusCommand(
    string DeviceId,
    bool IsOnline,
    DateTime LastSeen
) : IRequest<ApiResponse>;
