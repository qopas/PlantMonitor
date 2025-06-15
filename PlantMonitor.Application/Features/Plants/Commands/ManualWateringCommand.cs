using MediatR;
using Microsoft.EntityFrameworkCore;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.Plants.Commands;

public record ManualWateringCommand(
    long PlantId,
    int? DurationSeconds = null
) : IRequest<ApiResponse<string>>;

public class ManualWateringCommandHandler : IRequestHandler<ManualWateringCommand, ApiResponse<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceCommandsService _deviceCommands;

    public ManualWateringCommandHandler(IApplicationDbContext context, IDeviceCommandsService deviceCommands)
    {
        _context = context;
        _deviceCommands = deviceCommands;
    }

    public async Task<ApiResponse<string>> Handle(ManualWateringCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await _context.Plants
                .Include(p => p.Device)
                .FirstOrDefaultAsync(p => p.Id == request.PlantId, cancellationToken);

            if (plant == null)
            {
                return ApiResponse<string>.ErrorResult("Plant not found");
            }

            if (!plant.Device.IsOnline)
            {
                return ApiResponse<string>.ErrorResult("Device is offline. Cannot send watering command.");
            }

            var duration = request.DurationSeconds ?? plant.WateringDuration;
            var parameters = new { durationSeconds = duration };

            var command = await _deviceCommands.CreateCommandAsync(
                plant.DeviceId, 
                CommandType.ManualWatering, 
                parameters, 
                priority: 3 // High priority for manual commands
            );

            return ApiResponse<string>.SuccessResult($"Manual watering command sent. Duration: {duration} seconds. Command ID: {command.Id}");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.ErrorResult($"An error occurred: {ex.Message}");
        }
    }
}
