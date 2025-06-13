using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Plants.Queries;

public record GetPlantByDeviceIdQuery(long DeviceId) : IRequest<ApiResponse<PlantDto>>;

public record GetPlantByIdQuery(long Id) : IRequest<ApiResponse<PlantDto>>;
