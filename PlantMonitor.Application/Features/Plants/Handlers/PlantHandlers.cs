using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.Plants.Commands;
using PlantMonitor.Application.Features.Plants.Queries;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Application.Features.Plants.Handlers;

public class CreatePlantHandler : IRequestHandler<CreatePlantCommand, ApiResponse<PlantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePlantHandler> _logger;

    public CreatePlantHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ILogger<CreatePlantHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PlantDto>> Handle(CreatePlantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<PlantDto>.ErrorResult("Device not found");
            }

            if (device.Plant != null)
            {
                return ApiResponse<PlantDto>.ErrorResult("Device already has a plant configured");
            }

            var plant = new Plant
            {
                DeviceId = request.DeviceId,
                PlantName = request.PlantName,
                PlantType = request.PlantType,
                PlantSpecies = request.PlantSpecies,
                AgeWeeks = request.AgeWeeks,
                MoistureThresholdLow = request.MoistureThresholdLow,
                MoistureThresholdHigh = request.MoistureThresholdHigh,
                WateringDuration = request.WateringDuration,
                AutoWateringEnabled = request.AutoWateringEnabled,
                Notes = request.Notes
            };

            _context.Plants.Add(plant);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Plant {PlantName} created for device {DeviceId}", request.PlantName, request.DeviceId);

            var plantDto = _mapper.Map<PlantDto>(plant);
            return ApiResponse<PlantDto>.SuccessResult(plantDto, "Plant created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plant for device {DeviceId}", request.DeviceId);
            return ApiResponse<PlantDto>.ErrorResult("Internal server error");
        }
    }
}

public class GetPlantByDeviceIdHandler : IRequestHandler<GetPlantByDeviceIdQuery, ApiResponse<PlantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetPlantByDeviceIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PlantDto>> Handle(GetPlantByDeviceIdQuery request, CancellationToken cancellationToken)
    {
        var plant = await _context.Plants
            .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId, cancellationToken);

        if (plant == null)
        {
            return ApiResponse<PlantDto>.ErrorResult("Plant not found");
        }

        var plantDto = _mapper.Map<PlantDto>(plant);
        return ApiResponse<PlantDto>.SuccessResult(plantDto);
    }
}
public class UpdatePlantHandler(
    IApplicationDbContext context,
    IMapper mapper,
    ILogger<UpdatePlantHandler> logger)
    : IRequestHandler<UpdatePlantCommand, ApiResponse<PlantDto>>
{
    public async Task<ApiResponse<PlantDto>> Handle(UpdatePlantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await context.Plants
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (plant == null)
            {
                return ApiResponse<PlantDto>.ErrorResult("Plant not found");
            }

            
            if (!string.IsNullOrEmpty(request.PlantName))
            {
                plant.PlantName = request.PlantName;
            }

            if (!string.IsNullOrEmpty(request.PlantType))
            {
                plant.PlantType = request.PlantType;
            }

            if (request.MoistureThresholdLow.HasValue)
            {
                plant.MoistureThresholdLow = request.MoistureThresholdLow.Value;
            }

            if (request.MoistureThresholdHigh.HasValue)
            {
                plant.MoistureThresholdHigh = request.MoistureThresholdHigh.Value;
            }

            if (request.WateringDuration.HasValue)
            {
                plant.WateringDuration = request.WateringDuration.Value;
            }

            if (request.AutoWateringEnabled.HasValue)
            {
                plant.AutoWateringEnabled = request.AutoWateringEnabled.Value;
            }

            if (!string.IsNullOrEmpty(request.Notes))
            {
                plant.Notes = request.Notes;
            }

            plant.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Plant {PlantId} updated successfully", plant.Id);

            var plantDto = mapper.Map<PlantDto>(plant);
            return ApiResponse<PlantDto>.SuccessResult(plantDto, "Plant updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating plant {PlantId}", request.Id);
            return ApiResponse<PlantDto>.ErrorResult("Internal server error");
        }
    }
}
public class DeletePlantHandler(
    IApplicationDbContext context,
    ILogger<DeletePlantHandler> logger)
    : IRequestHandler<DeletePlantCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DeletePlantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await context.Plants
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (plant == null)
            {
                return ApiResponse.ErrorResult("Plant not found");
            }

            context.Plants.Remove(plant);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Plant {PlantId} deleted successfully", request.Id);

            return ApiResponse.SuccessResult("Plant deleted successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting plant {PlantId}", request.Id);
            return ApiResponse.ErrorResult("Internal server error");
        }
    }
}
public class GetPlantByIdHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<GetPlantByIdQuery, ApiResponse<PlantDto>>
{
    public async Task<ApiResponse<PlantDto>> Handle(GetPlantByIdQuery request, CancellationToken cancellationToken)
    {
        var plant = await context.Plants
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (plant == null)
        {
            return ApiResponse<PlantDto>.ErrorResult("Plant not found");
        }

        var plantDto = mapper.Map<PlantDto>(plant);
        return ApiResponse<PlantDto>.SuccessResult(plantDto);
    }
}