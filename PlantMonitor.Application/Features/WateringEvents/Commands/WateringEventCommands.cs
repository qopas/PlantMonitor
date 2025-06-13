using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.WateringEvents.Commands;

public record RecordWateringEventCommand(
    string DeviceId,
    DateTime Timestamp,
    TriggerType TriggerType,
    decimal? SoilMoistureBefore,
    decimal? SoilMoistureAfter,
    decimal? WaterLevelBefore,
    decimal? WaterLevelAfter,
    int DurationSeconds,
    bool WasSuccessful,
    string? FailureReason
) : IRequest<ApiResponse<WateringEventDto>>;

public record TriggerWateringCommand(long DeviceId, int? DurationSeconds = null) : IRequest<ApiResponse>;
