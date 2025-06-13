using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.WateringEvents.Queries;

public record GetWateringHistoryQuery(string DeviceId, DateTime? FromDate = null, DateTime? ToDate = null) : IRequest<ApiResponse<List<WateringEventDto>>>;

public record GetLatestWateringEventQuery(string DeviceId) : IRequest<ApiResponse<WateringEventDto>>;
