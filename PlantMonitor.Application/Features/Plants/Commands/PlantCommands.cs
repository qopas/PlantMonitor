using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Plants.Commands;

public record CreatePlantCommand(
    long DeviceId,
    string PlantName,
    string PlantType,
    string? PlantSpecies,
    int? AgeWeeks,
    int MoistureThresholdLow,
    int MoistureThresholdHigh,
    int WateringDuration,
    bool AutoWateringEnabled,
    string? Notes
) : IRequest<ApiResponse<PlantDto>>;

public record UpdatePlantCommand(
    long Id,
    string? PlantName,
    string? PlantType,
    int? MoistureThresholdLow,
    int? MoistureThresholdHigh,
    int? WateringDuration,
    bool? AutoWateringEnabled,
    string? Notes
) : IRequest<ApiResponse<PlantDto>>;

public record DeletePlantCommand(long Id) : IRequest<ApiResponse>;
