namespace PlantMonitor.Application.Common.Interfaces;

public interface IDeviceTokenService
{
    Task<string> GenerateTokenAsync(long deviceId, string? tokenName = null);
    Task<bool> ValidateTokenAsync(string token, long deviceId);
    Task RevokeTokenAsync(string tokenHash);
}
