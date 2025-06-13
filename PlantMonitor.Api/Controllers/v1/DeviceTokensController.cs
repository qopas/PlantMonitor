using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Auth.Commands;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class DeviceTokensController : BaseController
{
    /// <summary>
    /// Generate API token for device
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request)
    {
        var command = new GenerateDeviceTokenCommand(request.DeviceId, request.TokenName);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Validate device token (used by devices)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var command = new ValidateDeviceTokenCommand(request.Token, request.DeviceId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class GenerateTokenRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? TokenName { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}
