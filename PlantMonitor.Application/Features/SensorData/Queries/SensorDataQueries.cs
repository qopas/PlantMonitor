using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.SensorData.Queries;

public record GetLatestSensorDataQuery(string DeviceId) : IRequest<ApiResponse<SensorDataDto>>;

public record GetSensorDataHistoryQuery(string DeviceId, DateTime FromDate, DateTime ToDate) : IRequest<ApiResponse<List<SensorDataDto>>>;
