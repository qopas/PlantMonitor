using Microsoft.EntityFrameworkCore;
using PlantMonitor.Application.Common.Interfaces;

namespace PlantMonitor.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDeviceTokenService tokenService, IApplicationDbContext dbContext)
    {
        // Skip API key validation for certain endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (ShouldSkipApiKeyValidation(path))
        {
            await _next(context);
            return;
        }

        // Check for API key in Authorization header or query parameter
        var apiKey = GetApiKeyFromRequest(context);
        if (string.IsNullOrEmpty(apiKey))
        {
            await _next(context);
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipApiKeyValidation(string? path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        var skipPaths = new[]
        {
            "/api/v1/auth/",
            "/swagger",
            "/health",
            "/api/v1/devices/register"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }

    private static string? GetApiKeyFromRequest(HttpContext context)
    {
        // Check Authorization header (Bearer token)
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Check X-API-Key header
        var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKeyHeader))
        {
            return apiKeyHeader;
        }

        // Check query parameter
        return context.Request.Query["apiKey"].FirstOrDefault();
    }

    private static string? GetDeviceIdFromRequest(HttpContext context)
    {
        // Try to get device ID from route
        if (context.Request.RouteValues.TryGetValue("deviceId", out var routeDeviceId))
        {
            return routeDeviceId?.ToString();
        }

        // Try to get from request body (for POST requests)
        if (context.Request.HasJsonContentType() && context.Request.ContentLength > 0)
        {
            // This is a simplified approach - in a real implementation,
            // you might want to peek at the request body more carefully
            var deviceIdHeader = context.Request.Headers["X-Device-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceIdHeader))
            {
                return deviceIdHeader;
            }
        }

        // Try to get from query parameter
        return context.Request.Query["deviceId"].FirstOrDefault();
    }
}
