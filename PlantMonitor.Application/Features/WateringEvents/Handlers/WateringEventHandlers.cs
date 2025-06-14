using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.WateringEvents.Commands;
using PlantMonitor.Application.Features.WateringEvents.Queries;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.WateringEvents.Handlers;

public class RecordWateringEventHandler : IRequestHandler<RecordWateringEventCommand, ApiResponse<WateringEventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RecordWateringEventHandler> _logger;

    public RecordWateringEventHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<RecordWateringEventHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<WateringEventDto>> Handle(RecordWateringEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<WateringEventDto>.ErrorResult("Device not found");
            }

            var wateringEvent = new WateringEvent
            {
                DeviceId = device.Id,
                PlantId = device.Plant?.Id,
                Timestamp = request.Timestamp,
                TriggerType = request.TriggerType,
                SoilMoistureBefore = request.SoilMoistureBefore,
                SoilMoistureAfter = request.SoilMoistureAfter,
                WaterLevelBefore = request.WaterLevelBefore,
                WaterLevelAfter = request.WaterLevelAfter,
                DurationSeconds = request.DurationSeconds,
                WasSuccessful = request.WasSuccessful,
                FailureReason = request.FailureReason
            };

            _context.WateringEvents.Add(wateringEvent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Watering event recorded for device {DeviceId}, Duration: {Duration}s, Success: {Success}", 
                request.DeviceId, request.DurationSeconds, request.WasSuccessful);

            var wateringEventDto = _mapper.Map<WateringEventDto>(wateringEvent);
            return ApiResponse<WateringEventDto>.SuccessResult(wateringEventDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording watering event for device {DeviceId}", request.DeviceId);
            return ApiResponse<WateringEventDto>.ErrorResult("Internal server error");
        }
    }
}

public class GetWateringHistoryHandler : IRequestHandler<GetWateringHistoryQuery, ApiResponse<List<WateringEventDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetWateringHistoryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<WateringEventDto>>> Handle(GetWateringHistoryQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<List<WateringEventDto>>.ErrorResult("Device not found");
        }

        var query = _context.WateringEvents
            .Where(w => w.DeviceId == device.Id);

        if (request.FromDate.HasValue)
            query = query.Where(w => w.Timestamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(w => w.Timestamp <= request.ToDate.Value);

        var events = await query
            .OrderByDescending(w => w.Timestamp)
            .Take(100) // Limit to last 100 events
            .ToListAsync(cancellationToken);

        var eventDtos = _mapper.Map<List<WateringEventDto>>(events);
        return ApiResponse<List<WateringEventDto>>.SuccessResult(eventDtos);
    }
}
public class GetLatestWateringEventHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<GetLatestWateringEventQuery, ApiResponse<WateringEventDto>>
{
    public async Task<ApiResponse<WateringEventDto>> Handle(GetLatestWateringEventQuery request, CancellationToken cancellationToken)
    {
        var device = await context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

        if (device == null)
        {
            return ApiResponse<WateringEventDto>.ErrorResult("Device not found");
        }

        var latestEvent = await context.WateringEvents
            .Where(w => w.DeviceId == device.Id)
            .OrderByDescending(w => w.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestEvent == null)
        {
            return ApiResponse<WateringEventDto>.ErrorResult("No watering events found");
        }

        var eventDto = mapper.Map<WateringEventDto>(latestEvent);
        return ApiResponse<WateringEventDto>.SuccessResult(eventDto);
    }
}

public class TriggerWateringHandler : IRequestHandler<TriggerWateringCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TriggerWateringHandler> _logger;

    public TriggerWateringHandler(
        IApplicationDbContext context,
        ILogger<TriggerWateringHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(TriggerWateringCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse.ErrorResult("Device not found");
            }

            
            if (!device.IsOnline)
            {
                return ApiResponse.ErrorResult("Device is offline");
            }

            
            int duration = request.DurationSeconds ?? device.Plant?.WateringDuration ?? 10;

            
            var wateringEvent = new WateringEvent
            {
                DeviceId = device.Id,
                PlantId = device.Plant?.Id,
                Timestamp = DateTime.UtcNow,
                TriggerType = TriggerType.Manual,
                DurationSeconds = duration,
                WasSuccessful = true, // Assume successful for manual trigger
                Notes = "Manual watering triggered from mobile app"
            };

            _context.WateringEvents.Add(wateringEvent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Manual watering triggered for device {DeviceId}, Duration: {Duration}s", 
                device.DeviceId, duration);

            return ApiResponse.SuccessResult($"Manual watering triggered for {duration} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering manual watering for device {DeviceId}", request.DeviceId);
            return ApiResponse.ErrorResult("Internal server error");
        }
    }
}