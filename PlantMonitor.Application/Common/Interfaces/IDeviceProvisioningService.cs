namespace PlantMonitor.Application.Common.Interfaces;

public interface IDeviceProvisioningService
{
    Task<DeviceProvisioningResult> ProvisionNewDeviceAsync(string deviceId);
    Task<string> GenerateSecureTokenAsync(string deviceId);
    Task<bool> ValidateProvisionedTokenAsync(string token, string deviceId);
}

public class DeviceProvisioningResult
{
    public string DeviceId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
