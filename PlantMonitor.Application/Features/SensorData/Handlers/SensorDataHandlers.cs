using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.SensorData.Commands;
using PlantMonitor.Application.Features.SensorData.Queries;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.SensorData.Handlers;

public class RecordSensorDataHandler : IRequestHandler<RecordSensorDataCommand, ApiResponse<SensorDataDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RecordSensorDataHandler> _logger;
    private readonly INotificationService _notificationService;

    public RecordSensorDataHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<RecordSensorDataHandler> logger,
        INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<SensorDataDto>> Handle(RecordSensorDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<SensorDataDto>.ErrorResult("Device not found");
            }

            device.LastSeen = DateTime.UtcNow;
            device.IsOnline = true;

            var sensorData = new Domain.Entities.SensorData
            {
                DeviceId = device.Id,
                Timestamp = request.Timestamp,
                SoilMoisture = request.SoilMoisture,
                WaterLevel = request.WaterLevel,
                SoilMoistureRaw = request.SoilMoistureRaw,
                WaterLevelRaw = request.WaterLevelRaw,
                Temperature = request.Temperature,
                IsValid = request.IsValid
            };

            _context.SensorData.Add(sensorData);
            await CheckAndCreateAlerts(device, sensorData, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var sensorDataDto = _mapper.Map<SensorDataDto>(sensorData);
            return ApiResponse<SensorDataDto>.SuccessResult(sensorDataDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording sensor data for device {DeviceId}", request.DeviceId);
            return ApiResponse<SensorDataDto>.ErrorResult("Internal server error");
        }
    }

    private async Task CheckAndCreateAlerts(Device device, Domain.Entities.SensorData sensorData, CancellationToken cancellationToken)
    {
        
        if (sensorData.WaterLevel < 15m)
        {
            var existingAlert = await _context.DeviceAlerts
                .Where(a => a.DeviceId == device.Id && a.AlertType == AlertType.LowWater && !a.IsResolved)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAlert == null)
            {
                var alert = new DeviceAlert
                {
                    DeviceId = device.Id,
                    AlertType = AlertType.LowWater,
                    Severity = sensorData.WaterLevel < 5m ? AlertSeverity.Critical : AlertSeverity.Warning,
                    Title = "Low Water Level",
                    Message = "Water tank level is at {sensorData.WaterLevel:F1}%. Please refill soon."
                };

                _context.DeviceAlerts.Add(alert);
                await _notificationService.SendDeviceAlertAsync(
                    device.UserId, device.Id, alert.Title, alert.Message, alert.Severity);
            }
        }

        
        if (device.Plant != null && sensorData.SoilMoisture < device.Plant.MoistureThresholdLow - 10)
        {
            var existingAlert = await _context.DeviceAlerts
                .Where(a => a.DeviceId == device.Id && a.AlertType == AlertType.MoistureCritical && !a.IsResolved)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAlert == null)
            {
                var alert = new DeviceAlert
                {
                    DeviceId = device.Id,
                    AlertType = AlertType.MoistureCritical,
                    Severity = AlertSeverity.Error,
                    Title = "Critical Soil Moisture",
                    Message = "{device.Plant.PlantName} soil moisture is critically low at {sensorData.SoilMoisture:F1}%"
                };

                _context.DeviceAlerts.Add(alert);
                await _notificationService.SendPlantAlertAsync(
                    device.UserId, device.Plant.Id, alert.Title, alert.Message);
            }
        }
    }
}

public class GetLatestSensorDataHandler : IRequestHandler<GetLatestSensorDataQuery, ApiResponse<SensorDataDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLatestSensorDataHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<SensorDataDto>> Handle(GetLatestSensorDataQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<SensorDataDto>.ErrorResult("Device not found");
        }

        var latestData = await _context.SensorData
            .Where(s => s.DeviceId == device.Id && s.IsValid)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestData == null)
        {
            return ApiResponse<SensorDataDto>.ErrorResult("No sensor data found");
        }

        var sensorDataDto = _mapper.Map<SensorDataDto>(latestData);
        return ApiResponse<SensorDataDto>.SuccessResult(sensorDataDto);
    }
}
public class GetSensorDataHistoryHandler : IRequestHandler<GetSensorDataHistoryQuery, ApiResponse<List<SensorDataDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSensorDataHistoryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<SensorDataDto>>> Handle(GetSensorDataHistoryQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<List<SensorDataDto>>.ErrorResult("Device not found");
        }

        var query = _context.SensorData
            .Where(s => s.DeviceId == device.Id && s.IsValid)
            .Where(s => s.Timestamp >= request.FromDate && s.Timestamp <= request.ToDate);

        var sensorData = await query
            .OrderByDescending(s => s.Timestamp)
            .Take(1000) // Limit to last 1000 readings
            .ToListAsync(cancellationToken);

        var sensorDataDtos = _mapper.Map<List<SensorDataDto>>(sensorData);
        return ApiResponse<List<SensorDataDto>>.SuccessResult(sensorDataDtos);
    }
}
