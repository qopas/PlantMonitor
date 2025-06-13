using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.Alerts.Commands;
using PlantMonitor.Application.Features.Alerts.Queries;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Application.Features.Alerts.Handlers;

public class CreateDeviceAlertHandler(
    IApplicationDbContext context,
    IMapper mapper,
    ILogger<CreateDeviceAlertHandler> logger)
    : IRequestHandler<CreateDeviceAlertCommand, ApiResponse<DeviceAlertDto>>
{
    public async Task<ApiResponse<DeviceAlertDto>> Handle(CreateDeviceAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<DeviceAlertDto>.ErrorResult("Device not found");
            }

            var alert = new DeviceAlert
            {
                DeviceId = device.Id,
                AlertType = request.AlertType,
                Severity = request.Severity,
                Title = request.Title,
                Message = request.Message,
                IsResolved = false
            };

            context.DeviceAlerts.Add(alert);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Alert created for device {DeviceId}: {Title}", request.DeviceId, request.Title);

            var alertDto = mapper.Map<DeviceAlertDto>(alert);
            return ApiResponse<DeviceAlertDto>.SuccessResult(alertDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating alert for device {DeviceId}", request.DeviceId);
            return ApiResponse<DeviceAlertDto>.ErrorResult("Internal server error");
        }
    }
}

public class GetUserAlertsHandler : IRequestHandler<GetUserAlertsQuery, ApiResponse<List<DeviceAlertDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUserAlertsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<DeviceAlertDto>>> Handle(GetUserAlertsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DeviceAlerts
            .Include(a => a.Device)
            .Where(a => a.Device.UserId == request.UserId);

        if (!request.IncludeResolved)
        {
            query = query.Where(a => !a.IsResolved);
        }

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var alertDtos = _mapper.Map<List<DeviceAlertDto>>(alerts);
        return ApiResponse<List<DeviceAlertDto>>.SuccessResult(alertDtos);
    }
}

public class ResolveAlertHandler : IRequestHandler<ResolveAlertCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResolveAlertHandler> _logger;

    public ResolveAlertHandler(IApplicationDbContext context, ILogger<ResolveAlertHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var alert = await _context.DeviceAlerts
                .FirstOrDefaultAsync(a => a.Id == request.AlertId, cancellationToken);

            if (alert == null)
            {
                return ApiResponse.ErrorResult("Alert not found");
            }

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = request.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alert {AlertId} resolved by user {UserId}", request.AlertId, request.UserId);

            return ApiResponse.SuccessResult("Alert resolved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", request.AlertId);
            return ApiResponse.ErrorResult("Internal server error");
        }
    }
}
