using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.SensorData.Commands;

public record RecordSensorDataCommand(
    string DeviceId,
    DateTime Timestamp,
    decimal SoilMoisture,
    decimal WaterLevel,
    int? SoilMoistureRaw,
    int? WaterLevelRaw,
    decimal? Temperature,
    bool IsValid
) : IRequest<ApiResponse<SensorDataDto>>;
