using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using System.Security.Cryptography;

namespace PlantMonitor.Infrastructure.Services;

public class DeviceProvisioningService : IDeviceProvisioningService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeviceProvisioningService> _logger;

    public DeviceProvisioningService(IApplicationDbContext context, ILogger<DeviceProvisioningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DeviceProvisioningResult> ProvisionNewDeviceAsync(string deviceId)
    {
        try
        {
            var existingToken = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Device.DeviceId == deviceId && t.IsActive);

            if (existingToken != null)
            {
                return new DeviceProvisioningResult
                {
                    DeviceId = deviceId,
                    Token = string.Empty,
                    Success = false,
                    ErrorMessage = "Device already provisioned"
                };
            }

            var token = await GenerateSecureTokenAsync(deviceId);

            var device = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
            if (device == null)
            {
                device = new Device
                {
                    DeviceId = deviceId,
                    DeviceName = $"Plant Monitor {deviceId.Substring(deviceId.Length - 4)}",
                    DeviceType = Domain.Enums.DeviceType.ESP32PlantMonitor,
                    Status = Domain.Enums.DeviceStatus.Active,
                    UserId = 1
                };
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
            }

            var apiToken = new ApiToken
            {
                DeviceId = device.Id,
                TokenHash = BCrypt.Net.BCrypt.HashPassword(token),
                TokenName = "Factory Provisioned",
                IsActive = true
            };

            _context.ApiTokens.Add(apiToken);
            await _context.SaveChangesAsync();

            return new DeviceProvisioningResult
            {
                DeviceId = deviceId,
                Token = token,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning device {DeviceId}", deviceId);
            return new DeviceProvisioningResult
            {
                DeviceId = deviceId,
                Token = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> GenerateSecureTokenAsync(string deviceId)
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        var randomString = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return $"PM_{deviceId}_{randomString}";
    }

    public async Task<bool> ValidateProvisionedTokenAsync(string token, string deviceId)
    {
        var device = await _context.Devices
            .Include(d => d.ApiTokens)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device == null) return false;

        var activeTokens = device.ApiTokens.Where(t => t.IsActive);

        foreach (var storedToken in activeTokens)
        {
            if (BCrypt.Net.BCrypt.Verify(token, storedToken.TokenHash))
            {
                storedToken.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }
}
