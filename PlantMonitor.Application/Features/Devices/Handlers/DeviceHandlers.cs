using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.Devices.Commands;
using PlantMonitor.Application.Features.Devices.Queries;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.Devices.Handlers;

public class RegisterDeviceHandler : IRequestHandler<RegisterDeviceCommand, ApiResponse<DeviceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RegisterDeviceHandler> _logger;
    private readonly IDeviceTokenService _tokenService;

    public RegisterDeviceHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<RegisterDeviceHandler> logger,
        IDeviceTokenService tokenService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<DeviceDto>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (existingDevice != null)
            {
                return ApiResponse<DeviceDto>.ErrorResult("Device already registered");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.UserEmail, cancellationToken);

            if (user == null)
            {
                return ApiResponse<DeviceDto>.ErrorResult("User not found");
            }

            var device = new Device
            {
                DeviceId = request.DeviceId,
                UserId = user.Id,
                DeviceName = request.DeviceName ?? "Plant Monitor {request.DeviceId[^4..]}",
                DeviceType = DeviceType.ESP32PlantMonitor,
                Status = DeviceStatus.Active,
                IsOnline = true,
                LastSeen = DateTime.UtcNow
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync(cancellationToken);

            await _tokenService.GenerateTokenAsync(device.Id, "Initial Token");

            _logger.LogInformation("Device {DeviceId} registered for user {Email}", request.DeviceId, request.UserEmail);

            var deviceDto = _mapper.Map<DeviceDto>(device);
            return ApiResponse<DeviceDto>.SuccessResult(deviceDto, "Device registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {DeviceId}", request.DeviceId);
            return ApiResponse<DeviceDto>.ErrorResult("Internal server error");
        }
    }
}

public class GetUserDevicesHandler : IRequestHandler<GetUserDevicesQuery, ApiResponse<List<DeviceDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUserDevicesHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<DeviceDto>>> Handle(GetUserDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await _context.Devices
            .Include(d => d.Plant)
            .Where(d => d.UserId == request.UserId)
            .OrderByDescending(d => d.LastSeen)
            .ToListAsync(cancellationToken);

        var deviceDtos = _mapper.Map<List<DeviceDto>>(devices);
        return ApiResponse<List<DeviceDto>>.SuccessResult(deviceDtos);
    }
}

public class GetDeviceByDeviceIdHandler : IRequestHandler<GetDeviceByDeviceIdQuery, ApiResponse<DeviceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDeviceByDeviceIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<DeviceDto>> Handle(GetDeviceByDeviceIdQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<DeviceDto>.ErrorResult("Device not found");
        }

        var deviceDto = _mapper.Map<DeviceDto>(device);
        return ApiResponse<DeviceDto>.SuccessResult(deviceDto);
    }
}
public class GetDeviceByIdHandler : IRequestHandler<GetDeviceByIdQuery, ApiResponse<DeviceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDeviceByIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<DeviceDto>> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<DeviceDto>.ErrorResult("Device not found");
        }

        var deviceDto = _mapper.Map<DeviceDto>(device);
        return ApiResponse<DeviceDto>.SuccessResult(deviceDto);
    }
}
public class GetDeviceConfigHandler : IRequestHandler<GetDeviceConfigQuery, ApiResponse<object>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceConfigHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<object>> Handle(GetDeviceConfigQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<object>.ErrorResult("Device not found");
        }

        var config = new
        {
            deviceId = device.DeviceId,
            deviceName = device.DeviceName,
            plant = device.Plant != null ? new
            {
                plantName = device.Plant.PlantName,
                plantType = device.Plant.PlantType,
                moistureThresholdLow = device.Plant.MoistureThresholdLow,
                moistureThresholdHigh = device.Plant.MoistureThresholdHigh,
                wateringDuration = device.Plant.WateringDuration,
                autoWateringEnabled = device.Plant.AutoWateringEnabled
            } : null
        };

        return ApiResponse<object>.SuccessResult(config);
    }
}
public class UpdateDeviceHandler(
    IApplicationDbContext context,
    IMapper mapper,
    ILogger<UpdateDeviceHandler> logger)
    : IRequestHandler<UpdateDeviceCommand, ApiResponse<DeviceDto>>
{
    public async Task<ApiResponse<DeviceDto>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

            if (device == null)
            {
                return ApiResponse<DeviceDto>.ErrorResult("Device not found");
            }

            
            if (!string.IsNullOrEmpty(request.DeviceName))
            {
                device.DeviceName = request.DeviceName;
            }

            if (!string.IsNullOrEmpty(request.Location))
            {
                device.Location = request.Location;
            }

            if (!string.IsNullOrEmpty(request.FirmwareVersion))
            {
                device.FirmwareVersion = request.FirmwareVersion;
            }

            device.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Device {DeviceId} updated successfully", device.DeviceId);

            var deviceDto = mapper.Map<DeviceDto>(device);
            return ApiResponse<DeviceDto>.SuccessResult(deviceDto, "Device updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating device {DeviceId}", request.Id);
            return ApiResponse<DeviceDto>.ErrorResult("Internal server error");
        }
    }
}
public class UpdateDeviceStatusHandler : IRequestHandler<UpdateDeviceStatusCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateDeviceStatusHandler> _logger;

    public UpdateDeviceStatusHandler(
        IApplicationDbContext context,
        ILogger<UpdateDeviceStatusHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(UpdateDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse.ErrorResult("Device not found");
            }

            device.IsOnline = request.IsOnline;
            device.LastSeen = request.LastSeen;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Device {DeviceId} status updated - Online: {IsOnline}", 
                request.DeviceId, request.IsOnline);

            return ApiResponse.SuccessResult("Device status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device status {DeviceId}", request.DeviceId);
            return ApiResponse.ErrorResult("Internal server error");
        }
    }
}