using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlantMonitor.Application.Common.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PlantMonitor.Infrastructure.Authentication;

public class DeviceApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IDeviceProvisioningService _provisioningService;

    public DeviceApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IDeviceProvisioningService provisioningService)
        : base(options, logger, encoder, clock)
    {
        _provisioningService = provisioningService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Missing Authorization header");
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer "))
        {
            return AuthenticateResult.Fail("Invalid Authorization header format");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var deviceId = Request.Headers["X-Device-ID"].ToString();

        if (string.IsNullOrEmpty(deviceId))
        {
            return AuthenticateResult.Fail("Missing X-Device-ID header");
        }

        var isValid = await _provisioningService.ValidateProvisionedTokenAsync(token, deviceId);
        if (!isValid)
        {
            return AuthenticateResult.Fail("Invalid device token");
        }

        var claims = new[]
        {
            new Claim("device_id", deviceId),
            new Claim("token_type", "device_api_key")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
