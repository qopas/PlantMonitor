using MediatR;
using Microsoft.EntityFrameworkCore;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Application.Features.Devices.Commands;

public record ClaimDeviceCommand(
    string DeviceId,
    long UserId,
    string PlantName,
    string PlantType,
    string? Location = null
) : IRequest<ApiResponse<ClaimDeviceResponse>>;

public class ClaimDeviceCommandHandler : IRequestHandler<ClaimDeviceCommand, ApiResponse<ClaimDeviceResponse>>
{
    private readonly IApplicationDbContext _context;

    public ClaimDeviceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ClaimDeviceResponse>> Handle(ClaimDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find device by DeviceId
            var device = await _context.Devices
                .Include(d => d.Plant)
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<ClaimDeviceResponse>.ErrorResult("Device not found. Please check the Device ID.");
            }

            if (device.UserId != null)
            {
                return ApiResponse<ClaimDeviceResponse>.ErrorResult("Device is already claimed by another user.");
            }

            // Claim device for user
            device.UserId = request.UserId;
            device.DeviceName = $"{request.PlantName} Monitor";
            device.Status = DeviceStatus.Active;
            device.Location = request.Location;
            device.UpdatedAt = DateTime.UtcNow;

            // Create plant record
            var plant = new Plant
            {
                DeviceId = device.Id,
                PlantName = request.PlantName,
                PlantType = request.PlantType,
                MoistureThresholdLow = GetThresholdForPlantType(request.PlantType).Low,
                MoistureThresholdHigh = GetThresholdForPlantType(request.PlantType).High,
                WateringDuration = GetWateringDurationForPlantType(request.PlantType),
                AutoWateringEnabled = true
            };

            _context.Plants.Add(plant);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<ClaimDeviceResponse>.SuccessResult(new ClaimDeviceResponse
            {
                DeviceId = device.DeviceId,
                PlantId = plant.Id,
                PlantName = plant.PlantName,
                Message = "Device claimed successfully! You can now set up WiFi connection."
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<ClaimDeviceResponse>.ErrorResult($"An error occurred: {ex.Message}");
        }
    }

    private static (int Low, int High) GetThresholdForPlantType(string plantType)
    {
        return plantType.ToLower() switch
        {
            "snake plant" or "sansevieria" => (20, 60),
            "peace lily" or "spathiphyllum" => (40, 80),
            "money plant" or "pothos" => (30, 70),
            "aloe vera" => (15, 50),
            "fiddle leaf fig" => (30, 75),
            "spider plant" => (25, 65),
            "monstera" => (35, 75),
            _ => (30, 70) // Default
        };
    }

    private static int GetWateringDurationForPlantType(string plantType)
    {
        return plantType.ToLower() switch
        {
            "snake plant" or "aloe vera" => 5, // Less water for succulents
            "peace lily" or "monstera" => 12, // More water for tropical plants
            _ => 8 // Default duration
        };
    }
}

public class ClaimDeviceResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public long PlantId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
