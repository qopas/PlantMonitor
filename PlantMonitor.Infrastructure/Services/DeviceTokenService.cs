using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace PlantMonitor.Infrastructure.Services;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeviceTokenService> _logger;

    public DeviceTokenService(IApplicationDbContext context, ILogger<DeviceTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(long deviceId, string? tokenName = null)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = Convert.ToBase64String(tokenBytes);
        var tokenHash = ComputeHash(token);

        var apiToken = new ApiToken
        {
            DeviceId = deviceId,
            TokenHash = tokenHash,
            TokenName = tokenName ?? "API Token",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApiTokens.Add(apiToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Token generated for device {DeviceId}", deviceId);

        return token;
    }

    public async Task<bool> ValidateTokenAsync(string token, long deviceId)
    {
        try
        {
            var tokenHash = ComputeHash(token);

            var apiToken = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                         t.DeviceId == deviceId && 
                                         t.IsActive &&
                                         (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow));

            if (apiToken != null)
            {
                // Update last used time
                apiToken.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token for device {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task RevokeTokenAsync(string tokenHash)
    {
        var apiToken = await _context.ApiTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (apiToken != null)
        {
            apiToken.IsActive = false;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Token revoked for device {DeviceId}", apiToken.DeviceId);
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }
}
